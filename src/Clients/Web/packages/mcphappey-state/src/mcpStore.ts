import { create } from "zustand";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { MCPClientPool } from "mcphappey-mcp";

type State = {
  clients: Record<string, Client>;
  connect: (url: string) => Promise<void>;
  disconnect: (url: string) => void;
  addClient: (url: string, client: Client) => void;
  isConnected: (url: string) => boolean;
};

const pool = new MCPClientPool();

export const useMcpStore = create<State>((set, get) => ({
  clients: {},
  addClient: (url, client) =>
    set((state) => ({ clients: { ...state.clients, [url]: client } })),
  connect: async (url: string) => {
    if (get().clients[url]) return;
    const client = await pool.connect(url);
    set((state) => ({
      clients: { ...state.clients, [url]: client }
    }));
  },
  disconnect: (url: string) => {
    const client = get().clients[url];
    if (client) {
      pool.disconnect(url);
      set((state) => {
        const { [url]: _, ...rest } = state.clients;
        return { clients: rest };
      });
    }
  },
  isConnected: (url: string) => !!get().clients[url]
}));
