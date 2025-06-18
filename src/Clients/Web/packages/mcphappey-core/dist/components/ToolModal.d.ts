import type { McpToolInfo } from "mcphappey-types";
import "react-json-view-lite/dist/index.css";
type ToolModalProps = {
    show: boolean;
    tool: McpToolInfo | null;
    onClose: () => void;
    onSubmit: (values: Record<string, any>) => void;
    result?: any;
};
declare const ToolModal: ({ show, tool, onClose, onSubmit, result, }: ToolModalProps) => import("react/jsx-runtime").JSX.Element | null;
export default ToolModal;
//# sourceMappingURL=ToolModal.d.ts.map