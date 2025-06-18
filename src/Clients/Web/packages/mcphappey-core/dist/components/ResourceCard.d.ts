import type { McpResourceInfo } from "mcphappey-types";
type ResourceCardProps = {
    resource: McpResourceInfo;
    onRead: (resource: McpResourceInfo) => void;
    onOpenLink: (resource: McpResourceInfo) => void;
};
declare const ResourceCard: ({ resource, onRead, onOpenLink }: ResourceCardProps) => import("react/jsx-runtime").JSX.Element;
export default ResourceCard;
//# sourceMappingURL=ResourceCard.d.ts.map