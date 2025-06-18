// useMcpClient.ts
import { useCallback, useState } from "react";
import { useMcpClientContext } from "./McpClientContext";
/**
 * React hook to connect/disconnect to an MCP server by URL.
 * No auth logic included.
 */
export function useMcpClient(url, headers) {
    const { pool } = useMcpClientContext();
    const [client, setClient] = useState(undefined);
    const [connected, setConnected] = useState(false);
    const [connecting, setConnecting] = useState(false);
    const [error, setError] = useState(null);
    const connect = useCallback(async () => {
        if (connected || connecting)
            return;
        setConnecting(true);
        setError(null);
        try {
            const c = await pool.connect(url, headers);
            setClient(c);
            setConnected(true);
        }
        catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        }
        finally {
            setConnecting(false);
        }
    }, [url, headers, pool, connected, connecting]);
    const disconnect = useCallback(() => {
        pool.disconnect(url);
        setClient(undefined);
        setConnected(false);
        setError(null);
    }, [url, pool]);
    return {
        client,
        connected,
        connecting,
        error,
        connect,
        disconnect,
    };
}
//# sourceMappingURL=useMcpClient.js.map