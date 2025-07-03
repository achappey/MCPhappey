import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StreamableHTTPClientTransport } from "@modelcontextprotocol/sdk/client/streamableHttp.js";
import { SSEClientTransport } from "@modelcontextprotocol/sdk/client/sse.js";
import {
    CreateMessageRequest, CreateMessageRequestSchema, CreateMessageResult,
    ElicitRequest, ElicitRequestSchema, ElicitResult,
    LoggingMessageNotification, LoggingMessageNotificationSchema,
    ProgressNotification, ProgressNotificationSchema,
    type ServerCapabilities, type Tool, type Resource, type ResourceTemplate,
    CreateMessageResultSchema
} from "@modelcontextprotocol/sdk/types.js";
import { getMcpAccessToken, initiateMcpOAuthFlow, clearMcpAccessToken } from "mcphappeditor-auth";
import z from "zod";

export type McpConnectResult = {
    client: InstanceType<typeof Client>;
    capabilities: ServerCapabilities | null;
    systemInstructions?: string
    tools?: Tool[];
    setLogLevel?: any
    hasPrompts?: boolean;
    resources?: Resource[];
    resourceTemplates?: ResourceTemplate[];
};

export type SamplingCallback = (
    params: z.infer<typeof CreateMessageRequestSchema>["params"],
    accessToken: string
) => Promise<z.infer<typeof CreateMessageResultSchema>>;

// --- Internal utility: not exported ---
const toMcpClientName = (input: string): string => {
    return input.toLowerCase()
        .replace(/[\s_]+/g, '-')
        .replace(/[^a-z0-9-]/g, '')
        .replace(/-+/g, '-')
        .replace(/^-+|-+$/g, '');
}

const isTokenExpired = (token: string, skewSeconds = 60): boolean => {
    try {
        const [, payload] = token.split(".");
        if (!payload) return false;
        const { exp } = JSON.parse(atob(payload));
        if (typeof exp !== "number") return false;
        return exp * 1000 <= Date.now() + skewSeconds * 1000;
    } catch { return false; }
}

function needsOAuth(err: any): boolean {
    if (!err) return false;
    if (err.toString().includes("HTTP 401") || err.toString().includes("Non-200 status code (401)")) return true;
    if (err.status === 401 || err.status === 403) return true;
    if (typeof err === "object" && (err.error === "oauth_required" || err.code === "oauth_required")) return true;
    if (err.headers && typeof err.headers.get === "function") {
        const www = err.headers.get("WWW-Authenticate");
        if (www && www.includes("Bearer")) return true;
    }
    return false;
}

// --- Internal: core connect function ---
async function _connectMcpBase(
    url: string,
    type: "http" | "sse" = "http",
    opts: {
        token?: string;
        headers?: Record<string, string>;
        clientName?: string;
        clientVersion?: string;
        logLevel?: any;
        onSample?: (server: string, req: CreateMessageRequest) => Promise<CreateMessageResult>;
        onElicit?: (server: string, req: ElicitRequest) => Promise<ElicitResult>;
        onLogging?: (req: LoggingMessageNotification) => Promise<void>;
        onProgress?: (req: ProgressNotification) => Promise<void>;
        handleOAuth?: boolean;
    } = {}
): Promise<Client> {
    const headers: Record<string, string> = { ...opts.headers };
    let token: string | null = opts.token || getMcpAccessToken(url);

    if (token && isTokenExpired(token)) {
        clearMcpAccessToken(url);
        token = null;
    }
    if (token) headers["Authorization"] = `Bearer ${token}`;

    const client = new Client({
        name: toMcpClientName(opts.clientName || "test-client"),
        version: opts.clientVersion || "0.0.1",
        headers
    }, {
        capabilities: {
            sampling: opts.onSample ? {} : undefined,
            elicitation: opts.onElicit ? {} : undefined,
        }
    });

    if (opts.onSample) client.setRequestHandler(CreateMessageRequestSchema, req => opts.onSample!(url, req));
    if (opts.onElicit) client.setRequestHandler(ElicitRequestSchema, req => opts.onElicit!(url, req));
    if (opts.onLogging) client.setNotificationHandler(LoggingMessageNotificationSchema, ({ params }) => opts.onLogging!(params as any));
    if (opts.onProgress) client.setNotificationHandler(ProgressNotificationSchema, ({ params }) => opts.onProgress!(params as any));

    const baseUrl = new URL(url);
    const transport = type === "http"
        ? new StreamableHTTPClientTransport(baseUrl, { requestInit: { headers } })
        : new SSEClientTransport(baseUrl, { requestInit: { headers } });

    try {
        await client.connect(transport);
    } catch (err: any) {
        if (opts.handleOAuth && needsOAuth(err)) {
            await initiateMcpOAuthFlow(url);
            throw new Error("Redirecting for OAuth");
        }
        throw err;
    }

    if (opts.logLevel) await client.setLoggingLevel(opts.logLevel);

    return client;
}

// --- Public: minimal (just returns connected client) ---
export async function connectSimple(
    url: string,
    type: "http" | "sse" = "http",
    opts?: {
        token?: string;
        headers?: Record<string, string>;
        onSample?: (server: string, req: CreateMessageRequest) => Promise<CreateMessageResult>;
        clientName?: string;
        clientVersion?: string;
    }
): Promise<Client> {
    return _connectMcpBase(url, type, opts ?? {});
}

// --- Public: full-featured (returns client and server info) ---
export async function connectMcpServer(
    url: string,
    type: "http" | "sse" = "http",
    opts?: {
        token?: string;
        headers?: Record<string, string>;
        clientName?: string;
        clientVersion?: string;
        logLevel?: any;
        onSample?: (server: string, req: CreateMessageRequest) => Promise<CreateMessageResult>;
        onElicit?: (server: string, req: ElicitRequest) => Promise<ElicitResult>;
        onLogging?: (req: LoggingMessageNotification) => Promise<void>;
        onProgress?: (req: ProgressNotification) => Promise<void>;
        handleOAuth?: boolean;
    }
): Promise<{
    client: Client;
    capabilities: ServerCapabilities | null;
    tools?: Tool[];
    resources?: Resource[];
    resourceTemplates?: ResourceTemplate[];
    systemInstructions?: string;
    setLogLevel: any;
    hasPrompts: boolean;
}> {
    const client = await _connectMcpBase(url, type, opts ?? {});
    const capabilities = client.getServerCapabilities?.() ?? null;

    let tools: Tool[] | undefined;
    if (capabilities?.tools) {
        const res = await client.listTools();
        tools = res.tools;
    }

    let resources: Resource[] | undefined;
    let resourceTemplates: ResourceTemplate[] | undefined;
    if (capabilities?.resources) {
        const res = await client.listResources();
        resources = res.resources;
        const resemp = await client.listResourceTemplates();
        resourceTemplates = resemp.resourceTemplates;
    }

    return {
        client,
        capabilities,
        tools,
        setLogLevel: client.setLoggingLevel,
        hasPrompts: !!capabilities?.prompts,
        resources,
        resourceTemplates,
        systemInstructions: client.getInstructions()
    };
}
