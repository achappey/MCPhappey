import { Client } from "@modelcontextprotocol/sdk/client/index.js";
/**
 * MCPClientPool manages MCP Client instances by server URL.
 */
export declare class MCPClientPool {
    private clients;
    /** Connect (or return cached client) */
    connect(url: string, headers?: Record<string, string>): Promise<Client>;
    /**
     * Get a client by URL.
     */
    get(url: string): Client | undefined;
    /**
     * Disconnect and remove a client.
     */
    disconnect(url: string): void;
    /**
     * List all connected URLs.
     */
    list(): string[];
}
//# sourceMappingURL=clientPool.d.ts.map