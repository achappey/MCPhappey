import { createMcpClient, closeMcpClient } from "./createClient";
import { StreamableHTTPClientTransport } from "@modelcontextprotocol/sdk/client/streamableHttp.js";
/**
 * MCPClientPool manages MCP Client instances by server URL.
 */
export class MCPClientPool {
    clients = new Map();
    // private transports = new Map<string, StreamableHTTPClientTransport>();
    sessionIds = new Map();
    transports = new Map();
    /** Connect (or return cached client) */
    async connect(url, headers) {
        /* ───────── return cached client ───────── */
        if (this.clients.has(url))
            return this.clients.get(url);
        //console.log(crypto.randomUUID())
        // if (!this.sessionIds.has(url)) {
        // generate once; you could also use a prettier string
        // this.sessionIds.set(url, "YqiRpo2JqVoMbnV2H45Ttw");
        // }
        //  const sid = this.sessionIds.get(url)!
        /* ───────── build / reuse transport ────── */
        let transport = this.transports.get(url);
        if (!transport) {
            const opts = {
                requestInit: {
                    headers
                }
            };
            transport = new StreamableHTTPClientTransport(new URL(url), opts);
        }
        /* capture the first session id we get back from the server */
        /*  const originalSend = transport.send.bind(transport);
          transport.send = async (...args) => {
            console.log("blaaa")
      
            await originalSend(...args);
            console.log(transport)
            const id = transport!.sessionId;
            if (id) this.sessionIds.set(url, id);
          };
      */
        /* ───────── create MCP client (one-off) ── */
        const client = await createMcpClient(url, headers);
        await client.connect(transport);
        this.transports.set(url, transport);
        /* keep strong refs */
        this.clients.set(url, client);
        return client;
    }
    /**
     * Connect to a server and store the client.
     */
    async connect2(url, headers) {
        //if (this.clients.has(url)) {
        // return this.clients.get(url)!;
        //}
        const opts = {
            requestInit: { headers },
            sessionId: this.sessionIds.get(url) // may be undefined
        };
        console.log("blasdsadsadsa");
        console.log(this.sessionIds);
        const transport = new StreamableHTTPClientTransport(new URL(url), opts);
        const origSend = transport.send.bind(transport);
        transport.send = async (...args) => {
            await origSend(...args);
            const id = transport.sessionId;
            if (id)
                this.sessionIds.set(url, id); // cache after POST returns
        };
        const client = await createMcpClient(url, headers);
        await client.connect(transport);
        //this.clients.set(url, client);
        return client;
        /*
        let transport = this.transports.get(url);
        if (!transport) {
          transport = new StreamableHTTPClientTransport(new URL(url), {
            requestInit: { headers },
            sessionId: this.sessionIds.get(url)
            // keep sessionId issued by server
          });
    
          this.transports.set(url, transport);
        }
        // prevent SDK from closing the streaming connection
        await client.connect(transport);
        const originalClose = transport.close.bind(transport);
        transport.close = async () => {/* noop – keep session open */
        //};
        /*
        const client = await createMcpClient(url, headers);
        const httpTransport = new StreamableHTTPClientTransport(new URL(url),
          {
            requestInit: {
              headers: headers,
            },
            
          });
    
        await client.connect(httpTransport);
        await new Promise(resolve => setTimeout(resolve, 100))
        this.clients.set(url, client);
        return client;*/
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