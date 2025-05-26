// hooks/useMcpConnect.ts
import { useCallback } from "react";
import { useMcpPool } from "../context/McpPoolContext";
import { initiateOAuthFlow, getAccessToken } from "mcphappey-auth";
import { useMcpStore } from "mcphappey-state";
export const useMcpConnect = () => {
    const pool = useMcpPool();
    const { addClient } = useMcpStore();
    const connect = useCallback(async (url) => {
        const token = getAccessToken(url);
        try {
            const client = await pool.connect(url, token ? { Authorization: `Bearer ${token}` } : undefined);
            addClient(url, client); // centralise store update here
            return client;
        }
        catch (err) {
            if (err == `Error: Error POSTing to endpoint (HTTP 401): Invalid or missing token`) {
                await initiateOAuthFlow(url);
            }
            throw err;
        }
    }, [pool, addClient]);
    return { connect, pool };
};
//# sourceMappingURL=useMcpConnect.js.map