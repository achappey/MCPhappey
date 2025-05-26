// clientPool.ts
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { createMcpClient, closeMcpClient } from "./createClient";
import { StreamableHTTPClientTransport, StreamableHTTPClientTransportOptions } from "@modelcontextprotocol/sdk/client/streamableHttp.js";

/**
 * MCPClientPool manages MCP Client instances by server URL.
 */
export class MCPClientPool {
  private clients: Map<string, Client> = new Map();

  /** Connect (or return cached client) */
  async connect(
    url: string,
    headers?: Record<string, string>
  ): Promise<Client> {

    /* ───────── return cached client ───────── */
    if (this.clients.has(url)) return this.clients.get(url)!;
    const opts: StreamableHTTPClientTransportOptions = {
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
  get(url: string): Client | undefined {
    return this.clients.get(url);
  }

  /**
   * Disconnect and remove a client.
   */
  disconnect(url: string) {
    const client = this.clients.get(url);
    if (client) {
      closeMcpClient(client);
      this.clients.delete(url);
    }
  }

  /**
   * List all connected URLs.
   */
  list(): string[] {
    return Array.from(this.clients.keys());
  }
}
