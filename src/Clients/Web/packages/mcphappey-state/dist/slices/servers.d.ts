import type { McpCapabilitySummary, McpPromptInfo, McpResourceInfo, McpToolInfo } from "mcphappey-types";
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
export declare const useServerPrimitivesStore: import("zustand").UseBoundStore<import("zustand").StoreApi<ServerPrimitivesState & ServerPrimitivesActions>>;
export {};
//# sourceMappingURL=servers.d.ts.map