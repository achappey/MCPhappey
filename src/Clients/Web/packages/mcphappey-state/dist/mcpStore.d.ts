import { Client } from "@modelcontextprotocol/sdk/client/index.js";
type State = {
    clients: Record<string, Client>;
    connect: (url: string) => Promise<void>;
    disconnect: (url: string) => void;
    addClient: (url: string, client: Client) => void;
    isConnected: (url: string) => boolean;
};
export declare const useMcpStore: import("zustand").UseBoundStore<import("zustand").StoreApi<State>>;
export {};
//# sourceMappingURL=mcpStore.d.ts.map