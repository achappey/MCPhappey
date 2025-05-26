import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
import { JsonView, defaultStyles } from "react-json-view-lite";
import "react-json-view-lite/dist/index.css";
const ResourceModal = ({ show, onClose, resourceName, mimeType, content, loading, error, }) => {
    const { Modal, Spinner, Alert, Button } = useTheme();
    const renderContent = () => {
        if (loading)
            return _jsx(Spinner, { size: "sm" });
        if (error)
            return _jsx(Alert, { variant: "danger", children: error });
        if (content === null || content === undefined)
            return _jsx("span", { children: "No content" });
        // If the server already returned a JS object (or array), render it as JSON
        if (typeof content === "object") {
            return (_jsx(JsonView, { data: content, shouldExpandNode: () => true, style: defaultStyles }));
        }
        if (mimeType?.startsWith("image/") && content instanceof Blob) {
            const url = URL.createObjectURL(content);
            return _jsx("img", { src: url, alt: resourceName, style: { maxWidth: "100%" } });
        }
        // Try to parse JSON
        if (typeof content === "string" &&
            (mimeType?.includes("json") || mimeType?.includes("application/"))) {
            try {
                const json = JSON.parse(content);
                return (_jsx(JsonView, { data: json, shouldExpandNode: () => true, style: defaultStyles }));
            }
            catch {
                /* fall through */
            }
        }
        // Default: show as preformatted text
        return (_jsx("pre", { style: { whiteSpace: "pre-wrap", wordBreak: "break-word" }, children: typeof content === "string" ? content : String(content) }));
    };
    return (_jsxs(Modal, { show: show, onHide: onClose, size: "lg", title: resourceName, children: [_jsxs("div", { style: { marginBottom: 16, color: "#666" }, children: ["mimeType: ", mimeType] }), renderContent(), _jsx("div", { style: { marginTop: 24, display: "flex", gap: 8 }, children: _jsx(Button, { variant: "secondary", onClick: onClose, children: "Close" }) })] }));
};
export default ResourceModal;
//# sourceMappingURL=ResourceModal.js.map