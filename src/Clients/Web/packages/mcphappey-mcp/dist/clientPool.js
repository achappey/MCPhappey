import { createMcpClient, closeMcpClient } from "./createClient";
import { StreamableHTTPClientTransport } from "@modelcontextprotocol/sdk/client/streamableHttp.js";
/**
 * MCPClientPool manages MCP Client instances by server URL.
 */
export class MCPClientPool {
    clients = new Map();
    /** Connect (or return cached client) */
    async connect(url, headers) {
        /* ───────── return cached client ───────── */
        if (this.clients.has(url))
            return this.clients.get(url);
        const opts = {
            requestInit: {
                headers
            }
        };
        const transport = new StreamableHTTPClientTransport(new URL(url), opts);
        const client = await createMcpClient(url, headers);
        await client.connect(transport);
        this.clients.set(url, client);
        return client;
    }
    /**
     * Get a client by URL.
     */
    get(url) {
        return this.clients.get(url);
    }
    /**
     * Disconnect and remove a client.
     */
    disconnect(url) {
        const client = this.clients.get(url);
        if (client) {
            closeMcpClient(client);
            this.clients.delete(url);
        }
    }
    /**
     * List all connected URLs.
     */
    list() {
        return Array.from(this.clients.keys());
    }
}
//# sourceMappingURL=clientPool.js.map