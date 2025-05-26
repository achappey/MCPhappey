// createClient.ts
import { Client } from "@modelcontextprotocol/sdk/client/index.js";

/**
 * Create a new MCP Client instance and connect it to the given baseUrl.
 * @param baseUrl MCP server base URL
 * @param headers Optional HTTP headers (e.g., for auth)
 */
export async function createMcpClient(
  baseUrl: string | URL,
  headers?: Record<string, string>
): Promise<Client> {
  // Placeholder: actual implementation will use StreamableHTTPClientTransport
  return new Client({
    name: "web-client",
    version: "1.0.0",
    headers: headers ?? {},
  });
}

/**
 * Close an MCP Client connection.
 */
export function closeMcpClient(client: Client) {
  client.close?.();
}

/**
 * Check if the client is connected.
 */
export function isConnected(client: Client): boolean {
  // Placeholder: actual implementation may check client state
  return true;
}
