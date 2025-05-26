import type { McpPromptInfo } from "mcphappey-types";
import "react-json-view-lite/dist/index.css";
type PromptModalProps = {
    show: boolean;
    prompt: McpPromptInfo | null;
    onClose: () => void;
    onSubmit: (values: Record<string, string>) => void;
    result?: any;
};
declare const PromptModal: ({ show, prompt, onClose, onSubmit, result, }: PromptModalProps) => import("react/jsx-runtime").JSX.Element | null;
export default PromptModal;
//# sourceMappingURL=PromptModal.d.ts.map