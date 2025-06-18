/**
 * Hook to access and load server primitives for a given serverUrl.
 */
export declare function useServerPrimitives(serverUrl: string | null): {
    loadCapabilities: () => Promise<void>;
    loadPrompts: () => Promise<void>;
    loadResources: () => Promise<void>;
    loadTools: () => Promise<void>;
    loadAll: () => Promise<void>;
    capabilities?: import("mcphappey-types").McpCapabilitySummary;
    prompts?: import("mcphappey-types").McpPromptInfo[];
    resources?: import("mcphappey-types").McpResourceInfo[];
    tools?: import("mcphappey-types").McpToolInfo[];
    loading: boolean;
    error?: string;
};
//# sourceMappingURL=useServerPrimitives.d.ts.map