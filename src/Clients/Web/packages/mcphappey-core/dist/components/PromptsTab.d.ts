import type { McpCapabilitySummary, McpPromptInfo } from "mcphappey-types";
type PromptsTabProps = {
    serverUrl: string;
    capabilities: McpCapabilitySummary | null | undefined;
    prompts: McpPromptInfo[] | null | undefined;
    loading: boolean | undefined;
    error: string | null | undefined;
};
declare const PromptsTab: ({ serverUrl, capabilities, prompts, loading, error, }: PromptsTabProps) => import("react/jsx-runtime").JSX.Element;
export default PromptsTab;
//# sourceMappingURL=PromptsTab.d.ts.map