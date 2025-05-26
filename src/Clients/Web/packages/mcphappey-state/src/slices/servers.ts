import { create } from "zustand";
import type {
  McpCapabilitySummary,
  McpPromptInfo,
  McpResourceInfo,
  McpToolInfo,
} from "mcphappey-types";

type ServerPrimitives = {
  capabilities?: McpCapabilitySummary;
  prompts?: McpPromptInfo[];
  resources?: McpResourceInfo[];
  tools?: McpToolInfo[];
  loading: boolean;
  error?: string;
};

type ServerPrimitivesState = {
  servers: {
    [serverUrl: string]: ServerPrimitives;
  };
};

type ServerPrimitivesActions = {
  loadCapabilities: (url: string, client: any) => Promise<void>;
  loadPrompts: (url: string, client: any) => Promise<void>;
  loadResources: (url: string, client: any) => Promise<void>;
  loadTools: (url: string, client: any) => Promise<void>;
};

export const useServerPrimitivesStore = create<ServerPrimitivesState & ServerPrimitivesActions>((set, get) => ({
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
    } catch (e: any) {
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
    } catch (e: any) {
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
    } catch (e: any) {
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
    } catch (e: any) {
      set((s) => ({
        servers: {
          ...s.servers,
          [url]: { ...(s.servers[url] || { loading: false }), loading: false, error: e.message },
        },
      }));
    }
  },
}));
