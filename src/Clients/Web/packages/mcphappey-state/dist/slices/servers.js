import { create } from "zustand";
export const useServerPrimitivesStore = create((set, get) => ({
    servers: {},
    async loadCapabilities(url, client) {
        set((s) => ({
            servers: {
                ...s.servers,
                [url]: { ...(s.servers[url] || { loading: false }), loading: true, error: undefined },
            },
        }));
        try {
            const capabilities = await client.getServerCapabilities();
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), capabilities, loading: false },
                },
            }));
        }
        catch (e) {
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), loading: false, error: e.message },
                },
            }));
        }
    },
    async loadPrompts(url, client) {
        set((s) => ({
            servers: {
                ...s.servers,
                [url]: { ...(s.servers[url] || { loading: false }), loading: true, error: undefined },
            },
        }));
        try {
            const { prompts } = await client.listPrompts();
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), prompts, loading: false },
                },
            }));
        }
        catch (e) {
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), loading: false, error: e.message },
                },
            }));
        }
    },
    async loadResources(url, client) {
        set((s) => ({
            servers: {
                ...s.servers,
                [url]: { ...(s.servers[url] || { loading: false }), loading: true, error: undefined },
            },
        }));
        try {
            const { resources } = await client.listResources();
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), resources, loading: false },
                },
            }));
        }
        catch (e) {
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), loading: false, error: e.message },
                },
            }));
        }
    },
    async loadTools(url, client) {
        set((s) => ({
            servers: {
                ...s.servers,
                [url]: { ...(s.servers[url] || { loading: false }), loading: true, error: undefined },
            },
        }));
        try {
            const { tools } = await client.listTools();
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), tools, loading: false },
                },
            }));
        }
        catch (e) {
            set((s) => ({
                servers: {
                    ...s.servers,
                    [url]: { ...(s.servers[url] || { loading: false }), loading: false, error: e.message },
                },
            }));
        }
    },
}));
//# sourceMappingURL=servers.js.map