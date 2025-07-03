import type { StateCreator } from "zustand";
import {
  connectMcpServer, McpConnectResult,
  CreateMessageRequest, CreateMessageResult,
  type CallToolResult, type ReadResourceResult,
  type ServerCapabilities, type LoggingMessageNotification,
  ProgressNotification, Prompt, ElicitRequest,
  Resource, ResourceTemplate, ElicitResult
} from "mcphappeditor-mcp";

type McpStatus = "idle" | "connecting" | "connected" | "error";
type LogLevel = "error" | "debug" | "info" | "notice" | "warning" | "critical" | "alert" | "emergency";

type Tool = {
  name: string;
  description?: string;
  inputSchema: Record<string, unknown>;
  outputSchema?: Record<string, unknown>;
};

type McpConnectOpts = {
  token?: string;
  headers?: Record<string, string>;
  onSample?: (server: string, req: CreateMessageRequest) => Promise<CreateMessageResult>;
  onElicit?: (server: string, req: ElicitRequest) => Promise<ElicitResult>;
  onLogging?: (notif: LoggingMessageNotification) => Promise<void>;
  onProgress?: (notif: ProgressNotification) => Promise<void>;
};

export type ResourceResult = { uri: string; data: ReadResourceResult };
export type SamplingRequest = [string, CreateMessageRequest, CreateMessageResult];
export type ElicitRequestItem = [string, ElicitRequest, ElicitResult];

export type McpSlice = {
  clients: Record<string, McpConnectResult["client"]>;
  status: Record<string, McpStatus>;
  mcpErrors: Record<string, string | null>;
  capabilities: Record<string, ServerCapabilities | null>;
  tools: Record<string, Tool[]>;
  prompts: Record<string, Prompt[]>;
  resources: Record<string, Resource[]>;
  resourceTemplates: Record<string, ResourceTemplate[]>;
  mcpInstructions: Record<string, string>;
  resourceResults: ResourceResult[];
  notifications: LoggingMessageNotification[];
  logLevel: LogLevel;
  addNotification: (notif: LoggingMessageNotification) => void;
  clearNotifications: () => void;
  progress: ProgressNotification[];
  addProgress: (notif: ProgressNotification) => void;
  clearProgress: () => void;

  elicit: Record<string, ElicitRequestItem>;
  addElicitRequest: (id: string, server: string, notif: ElicitRequest) => void;
  addElicitResponse: (id: string, server: string, notif: ElicitResult) => void;
  clearElicit: () => void;

  sampling: Record<string, SamplingRequest>;
  addSamplingRequest: (id: string, server: string, notif: CreateMessageRequest) => void;
  addSamplingResponse: (id: string, server: string, notif: CreateMessageResult) => void;
  clearSampling: () => void;

  tokens: Record<string, string>;
  setToken: (url: string, token: string) => void;
  clearToken: (url: string) => void;
  readResource: (uri: string) => Promise<void>;
  callTool: (toolCallId: string, name: string, parameters: any, signal?: AbortSignal) => Promise<CallToolResult | undefined>;
  removeResourceResult: (uri: string) => void;
  clearResourceResults: () => void;
  connect: (clientName: string, clientVersion: string, url: string, type: "http" | "sse", opts?: McpConnectOpts) => Promise<void>;
  disconnect: (url: string) => void;
  refreshTools: (url: string) => Promise<void>;
  refreshPrompts: (url: string) => Promise<void>;
  hasPrompts: (url: string) => boolean;
  refreshResources: (url: string) => Promise<void>;
  setLogLevel: (logLevel: LogLevel) => Promise<void>;
};

export const createMcpSlice: StateCreator<
  any,
  [],
  [],
  McpSlice
> = (set, get, store) => ({
  clients: {},
  status: {},
  mcpErrors: {},
  capabilities: {},
  tools: {},
  prompts: {},
  resources: {},
  resourceTemplates: {},
  mcpInstructions: {},
  resourceResults: [],
  notifications: [],
  logLevel: "info",
  sampling: {},
  elicit: {},
  progress: [],
  addElicitRequest: (id, server, notif) =>
    set((state: any) => ({
      elicit: { ...state.elicit, [id]: [server, notif] }
    })),
  addElicitResponse: (id, server, notif) =>
    set((state: any) => ({
      elicit: {
        ...state.elicit,
        [id]: state.elicit[id] ? [server, state.elicit[id][1], notif]
          : [server, undefined, notif]
      }
    })),
  clearElicit: () =>
    set((state: any) => ({
      elicit: {}
    })),
  addSamplingRequest: (id, server, notif) =>
    set((state: any) => ({
      sampling: { ...state.sampling, [id]: [server, notif] }
    })),
  addSamplingResponse: (id, server, notif) =>
    set((state: any) => ({
      sampling: {
        ...state.sampling,
        [id]: state.sampling[id] ? [server, state.sampling[id][1], notif] : [server, undefined, notif]
      }
    })),
  clearSampling: () =>
    set((state: any) => ({
      sampling: {}
    })),
  addNotification: (notif) =>
    set((state: any) => ({
      notifications: [...state.notifications, notif]
    })),
  clearNotifications: () =>
    set((state: any) => {
      return { notifications: [] };
    }),
  addProgress: (notif) =>
    set((state: any) => ({
      progress: [...state.progress, notif]
    })),
  clearProgress: () =>
    set((state: any) => {
      return { progress: [] };
    }),
  tokens: {},
  setToken: (url, token) => {
    set((state: any) => ({
      tokens: { ...state.tokens, [url]: token }
    }));
  },
  clearToken: (url) => {
    set((state: any) => {
      const newTokens = { ...state.tokens };
      delete newTokens[url];
      return { tokens: newTokens };
    });
  },
  readResource: async (uri: string) => {
    // Find the server URL that owns this resource
    const { resources, clients, resourceResults } = get();
    const url = Object.keys(resources).find(url =>
      (resources[url] || []).some((r: Resource) => r.uri === uri)
    );
    if (!url) return;
    const client = clients[url];
    if (!client?.readResource) return;
    // Prevent duplicate
    if (resourceResults.some((r: ResourceResult) => r.uri === uri)) return;
    try {
      const res: ReadResourceResult = await client.readResource({ uri });
      console.log(res)
      set((state: any) => ({
        resourceResults: [...state.resourceResults, { uri, data: res }]
      }));
    } catch (e: any) {
      set((state: any) => ({
        mcpErrors: { ...state.mcpErrors, [url]: "Failed to read resource: " + (e?.message || String(e)) }
      }));
    }
  },
  setLogLevel: async (logLevel: string) => {
    const { clients } = get();

    await Promise.all(
      Object.values(clients).map((client: any) => client.setLoggingLevel(logLevel))
    );

    set((state: any) => ({
      logLevel: logLevel
    }));

  },
  callTool: async (toolCallId: string, name: string, parameters: any, signal?: AbortSignal) => {
    // Find the server URL that owns this resource
    const { clients, tools } = get();
    const url = Object.keys(tools).find(url =>
      (tools[url] || []).some((r: Tool) => r.name === name)
    );
    if (!url) return;
    const client = clients[url];
    if (!client?.callTool) return;

    try {
      const res: CallToolResult = await client.callTool({
        name: name,
        arguments: JSON.parse(parameters),
        resetTimeoutOnProgress: true,
        _meta: {
          progressToken: toolCallId
        }
      }, undefined, {
        signal: signal
      });

      return res;
    } catch (e: any) {
      set((state: any) => ({
        mcpErrors: { ...state.mcpErrors, [url]: "Failed to call tool: " + (e?.message || String(e)) }
      }));
    }
    return undefined;
  },
  removeResourceResult: (uri: string) => {
    set((state: any) => ({
      resourceResults: state.resourceResults.filter((r: ResourceResult) => r.uri !== uri)
    }));
  },
  clearResourceResults: () => {
    set((state: any) => ({
      resourceResults: []
    }));
  },
  connect: async (clientName, clientVersion, url, type, opts) => {
    const { status, tokens, logLevel } = get();
    if (status[url] === "connecting" || status[url] === "connected") return;
    set((state: any) => ({
      status: { ...state.status, [url]: "connecting" },
      mcpErrors: { ...state.mcpErrors, [url]: null },
    }));

    try {
      // Prefer token from state if not explicitly passed
      const token = opts?.token ?? tokens[url];
      const connectOpts: McpConnectOpts = {
        ...opts, token,
        onSample: opts?.onSample,
        onElicit: opts?.onElicit,
        onLogging: opts?.onLogging,
        onProgress: opts?.onProgress,
      };

      const { client, capabilities, tools, hasPrompts, resources,
        resourceTemplates, systemInstructions } = await connectMcpServer(
          url,
          type,
          {
            clientName: clientName ?? "web-client",
            clientVersion,
            logLevel,
            handleOAuth: true,
            ...connectOpts // if you have additional opts, spread them here
          }
        );


      set((state: any) => ({
        clients: { ...state.clients, [url]: client },
        status: { ...state.status, [url]: "connected" },
        capabilities: { ...state.capabilities, [url]: capabilities },
        tools: { ...state.tools, [url]: tools ?? [] },
        prompts: { ...state.prompts, [url]: hasPrompts ? [] : undefined },
        resources: { ...state.resources, [url]: resources ?? [] },
        resourceTemplates: { ...state.resourceTemplates, [url]: resourceTemplates ?? [] },
        mcpInstructions: { ...state.mcpInstructions, [url]: systemInstructions ?? "" },
      }));
    } catch (err: any) {
      set((state: any) => ({
        status: { ...state.status, [url]: "error" },
        mcpErrors: { ...state.mcpErrors, [url]: err?.message || String(err) },
      }));
    }
  },
  disconnect: (url) => {
    const { clients } = get();
    if (clients[url]) {
      clients[url].close?.();
      const newClients = { ...clients };
      delete newClients[url];
      set((state: any) => ({
        clients: newClients,
        status: { ...state.status, [url]: "idle" },
        capabilities: { ...state.capabilities, [url]: null },
        tools: { ...state.tools, [url]: [] },
        prompts: { ...state.prompts, [url]: [] },
        resources: { ...state.resources, [url]: [] },
        resourceTemplates: { ...state.resourceTemplates, [url]: [] },
        mcpInstructions: { ...state.systemInstructions, [url]: "" },
      }));
    }
  },
  refreshTools: async (url) => {
    const { clients } = get();
    const client = clients[url];
    if (!client) return;
    try {
      const res = await client.listTools();
      set((state: any) => ({
        tools: { ...state.tools, [url]: res.tools },
      }));
    } catch (e) {
      set((state: any) => ({
        mcpErrors: { ...state.mcpErrors, [url]: "Failed to fetch tools" },
      }));
    }
  },
  hasPrompts: (url) => {
    const { clients, prompts } = get();
    const client = clients[url];
    return client && prompts[url] != undefined
  },
  refreshPrompts: async (url) => {
    const { clients, prompts } = get();
    const client = clients[url];
    if (!client || !prompts[url]) return;
    try {
      const res = await client.listPrompts();
      set((state: any) => ({
        prompts: { ...state.prompts, [url]: res.prompts },
      }));
    } catch (e) {
      set((state: any) => ({
        mcpErrors: { ...state.mcpErrors, [url]: "Failed to fetch prompts" },
      }));
    }
  },
  refreshResources: async (url) => {
    const { clients } = get();
    const client = clients[url];
    if (!client) return;
    try {
      const res = await client.listResources();
      set((state: any) => ({
        resources: { ...state.resources, [url]: res.resources },
      }));
    } catch (e) {
      set((state: any) => ({
        mcpErrors: { ...state.mcpErrors, [url]: "Failed to fetch resources" },
      }));
    }
  },
});
