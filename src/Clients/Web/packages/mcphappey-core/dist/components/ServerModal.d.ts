import type { McpServerWithName } from "mcphappey-types";
interface ServerModalProps {
    show: boolean;
    server: McpServerWithName | null;
    onClose: () => void;
}
declare const ServerModal: ({ show, server, onClose }: ServerModalProps) => import("react/jsx-runtime").JSX.Element | null;
export default ServerModal;
//# sourceMappingURL=ServerModal.d.ts.map