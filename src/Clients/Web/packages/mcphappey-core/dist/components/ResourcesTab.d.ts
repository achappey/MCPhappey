import type { McpCapabilitySummary, McpResourceInfo } from "mcphappey-types";
type ResourcesTabProps = {
    serverUrl: string;
    capabilities: McpCapabilitySummary | null | undefined;
    resources: McpResourceInfo[] | null | undefined;
    loading: boolean | undefined;
    error: string | null | undefined;
};
declare const ResourcesTab: ({ serverUrl, capabilities, resources, loading, error, }: ResourcesTabProps) => import("react/jsx-runtime").JSX.Element;
export default ResourcesTab;
//# sourceMappingURL=ResourcesTab.d.ts.map