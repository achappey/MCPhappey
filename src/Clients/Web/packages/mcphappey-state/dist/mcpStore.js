import { create } from "zustand";
import { MCPClientPool } from "mcphappey-mcp";
const pool = new MCPClientPool();
export const useMcpStore = create((set, get) => ({
    clients: {},
    addClient: (url, client) => set((state) => ({ clients: { ...state.clients, [url]: client } })),
    connect: async (url) => {
        if (get().clients[url])
            return;
        const client = await pool.connect(url);
        set((state) => ({
            clients: { ...state.clients, [url]: client }
        }));
    },
    disconnect: (url) => {
        const client = get().clients[url];
        if (client) {
            pool.disconnect(url);
            set((state) => {
                const { [url]: _, ...rest } = state.clients;
                return { clients: rest };
            });
        }
    },
    isConnected: (url) => !!get().clients[url]
}));
//# sourceMappingURL=mcpStore.js.map