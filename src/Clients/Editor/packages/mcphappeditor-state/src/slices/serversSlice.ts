import type { StateCreator } from "zustand";

export type ServerConfig = {
  type: "http" | "sse"; url: string;
  headers?: Record<string, string>
};


export type ServersSlice = {
  // List of MCP server JSON endpoints (subscriptions)
  endpoints: string[];
  addEndpoint: (url: string) => void;
  removeEndpoint: (url: string) => void;
  resetEndpoints: () => void;
  setEndpoints: (urls: string[]) => void;

  // All loaded servers (manual + remote)
  servers: Record<string, ServerConfig>;
  selected: string[];
  addServer: (name: string, cfg: ServerConfig) => void;
  removeServer: (name: string) => void;
  selectServers: (names: string[]) => void;
  resetServers: () => void;

  // Refresh all remote servers from endpoints
  refreshRemoteServers: () => Promise<void>;
};

export const createServersSlice: StateCreator<
  any,
  [],
  [],
  ServersSlice
> = (set, get, store) => ({
  endpoints: [],
  servers: {},
  selected: [],
  addEndpoint: (url) =>
    set((s: any) => {
      if (s.endpoints.includes(url)) return {};
      return { endpoints: [...s.endpoints, url] };
    }),
  removeEndpoint: (url) =>
    set((s: any) => ({
      endpoints: s.endpoints.filter((e: string) => e !== url),
    })),
  resetEndpoints: () =>
    set(() => ({
      endpoints: [],
    })),
  setEndpoints: (urls) =>
    set(() => ({
      endpoints: urls,
    })),
  addServer: (name, cfg) =>
    set((s: any) => ({
      servers: { ...s.servers, [name]: cfg },
    })),
  removeServer: (name) =>
    set((s: any) => {
      const { [name]: _, ...rest } = s.servers;
      const filtered = s.selected.filter((n: string) => n !== name);
      return {
        servers: rest,
        selected: filtered,
      };
    }),
  selectServers: (names) =>
    set(() => ({
      selected: names,
    })),
  resetServers: () =>
    set(() => ({
      servers: {},
      selected: [],
    })),
  refreshRemoteServers: async () => {
    const { endpoints, servers } = get();
    if (!endpoints?.length) return;
    // Fetch all endpoints in parallel
    const results = await Promise.all(
      endpoints.map(async (url: string) => {
        try {
          const res = await fetch(url);
          if (!res.ok) return null;
          return await res.json();
        } catch {
          return null;
        }
      })
    );
    // Flatten and process the combined results
    results
      .filter(Boolean)
      .flat()
      .forEach((item: any) => {
        const record =
          item && typeof item === "object" && "servers" in item ? item.servers : item;
        Object.entries(record as Record<string, any>).forEach(([name, cfg]) => {
          // Only add if not already present (manual servers take precedence)
          if (!servers[name]) {
            get().addServer(name, cfg);
          }
        });
      });
  },
});
