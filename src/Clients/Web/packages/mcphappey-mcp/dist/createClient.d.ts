import { Client } from "@modelcontextprotocol/sdk/client/index.js";
/**
 * Create a new MCP Client instance and connect it to the given baseUrl.
 * @param baseUrl MCP server base URL
 * @param headers Optional HTTP headers (e.g., for auth)
 */
export declare function createMcpClient(baseUrl: string | URL, headers?: Record<string, string>): Promise<Client>;
/**
 * Close an MCP Client connection.
 */
export declare function closeMcpClient(client: Client): void;
/**
 * Check if the client is connected.
 */
export declare function isConnected(client: Client): boolean;
//# sourceMappingURL=createClient.d.ts.map