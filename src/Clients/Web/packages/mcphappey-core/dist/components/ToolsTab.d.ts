import type { McpCapabilitySummary, McpToolInfo } from "mcphappey-types";
type ToolsTabProps = {
    serverUrl: string;
    capabilities: McpCapabilitySummary | null | undefined;
    tools: McpToolInfo[] | null | undefined;
    loading: boolean | undefined;
    error: string | null | undefined;
};
declare const ToolsTab: ({ serverUrl, capabilities, tools, loading, error, }: ToolsTabProps) => import("react/jsx-runtime").JSX.Element;
export default ToolsTab;
//# sourceMappingURL=ToolsTab.d.ts.map