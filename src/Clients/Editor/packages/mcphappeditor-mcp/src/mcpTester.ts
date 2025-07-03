import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StreamableHTTPClientTransport } from "@modelcontextprotocol/sdk/client/streamableHttp.js";
import { SSEClientTransport } from "@modelcontextprotocol/sdk/client/sse.js";

/**
 * Tries to connect to an MCP server and tells you the result.
 *
 * @returns { "ok" | "unauthorized" | "error" }
 */
export async function connectionTest(
  url: string,
  type: "http" | "sse" = "http",
  opts?: {
    token?: string;
    headers?: Record<string, string>;
    clientName?: string;
    clientVersion?: string;
  }
): Promise<"ok" | "unauthorized" | "error"> {
  // Use whatever header logic you want
  const headers: Record<string, string> = { ...opts?.headers };
  if (opts?.token) headers["Authorization"] = `Bearer ${opts.token}`;

  const client = new Client({
    name: opts?.clientName || "test-client",
    version: opts?.clientVersion || "0.0.1",
    headers,
  });

  const baseUrl = new URL(url);
  const transport =
    type === "http"
      ? new StreamableHTTPClientTransport(baseUrl, { requestInit: { headers } })
      : new SSEClientTransport(baseUrl, { requestInit: { headers } });

  try {
    await client.connect(transport);
    // If connect succeeds, we call it OK.
    return "ok";
  } catch (err: any) {
    // Typical 401/403 = Unauthorized = valid MCP server
    if (
      err?.status === 401 ||
      err?.status === 403 ||
      err?.message?.toString().includes("401") ||
      err?.toString().includes("401") ||
      (typeof err === "object" &&
        (err.error === "oauth_required" || err.code === "oauth_required"))
    ) {
      return "unauthorized";
    }
    // All other errors: treat as "error"
    return "error";
  }
  finally {
    await client.close();

  }
}
