import type { McpServerWithName } from "mcphappey-types";
interface ServerCardProps {
    server: McpServerWithName;
    onShowDetails: (server: McpServerWithName) => void;
}
declare const ServerCard: ({ server, onShowDetails }: ServerCardProps) => import("react/jsx-runtime").JSX.Element;
export default ServerCard;
//# sourceMappingURL=ServerCard.d.ts.map