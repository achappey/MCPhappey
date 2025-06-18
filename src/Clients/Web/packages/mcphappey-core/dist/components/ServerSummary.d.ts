import type { McpServerWithName, McpCapabilitySummary } from "mcphappey-types";
type ServerSummaryProps = {
    server: McpServerWithName;
    loading: boolean;
    error: string | null | undefined;
    promptCount?: number;
    toolCount?: number;
    resourceCount?: number;
    capabilities: McpCapabilitySummary | null | undefined;
};
declare const ServerSummary: ({ server, loading, promptCount, resourceCount, toolCount, error, capabilities, }: ServerSummaryProps) => import("react/jsx-runtime").JSX.Element;
export default ServerSummary;
//# sourceMappingURL=ServerSummary.d.ts.map