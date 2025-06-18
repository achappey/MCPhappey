import { Client } from "@modelcontextprotocol/sdk/client/index.js";
/**
 * React hook to connect/disconnect to an MCP server by URL.
 * No auth logic included.
 */
export declare function useMcpClient(url: string, headers?: Record<string, string>): {
    client: Client<{
        method: string;
        params?: {
            [x: string]: unknown;
            _meta?: {
                [x: string]: unknown;
                progressToken?: string | number | undefined;
            } | undefined;
        } | undefined;
    }, {
        method: string;
        params?: {
            [x: string]: unknown;
            _meta?: {
                [x: string]: unknown;
            } | undefined;
        } | undefined;
    }, {
        [x: string]: unknown;
        _meta?: {
            [x: string]: unknown;
        } | undefined;
    }> | undefined;
    connected: boolean;
    connecting: boolean;
    error: string | null;
    connect: () => Promise<void>;
    disconnect: () => void;
};
//# sourceMappingURL=useMcpClient.d.ts.map