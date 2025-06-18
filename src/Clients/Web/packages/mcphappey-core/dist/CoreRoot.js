import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
// Root component for MCP Happey apps: loads server lists, manages state, renders server list UI.
// Requires a ThemeProvider (throws if missing).
import { useEffect, useState } from "react";
import { useAppStore } from "mcphappey-state";
import { useTheme } from "./ThemeContext";
import ServerList from "./components/ServerList";
import { McpPoolProvider } from "./context/McpPoolContext";
export const CoreRoot = ({ initialLists = [], allowCustomLists = true, }) => {
    //return (<div>Hello from CoreRoot</div>)
    const theme = useTheme(); // Throws if no provider
    const { Alert, Button, Spinner, Input } = theme;
    const { servers, loading, error, importList, clearAll } = useAppStore();
    const [customUrl, setCustomUrl] = useState("");
    // Import initial lists on mount (only once)
    useEffect(() => {
        if (initialLists.length > 0) {
            initialLists.forEach((url) => importList(url));
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    const handleAddCustom = (e) => {
        e.preventDefault();
        if (customUrl.trim()) {
            importList(customUrl.trim());
            setCustomUrl("");
        }
    };
    return (_jsx(McpPoolProvider, { children: _jsxs("div", { role: "main", style: { maxWidth: 900, margin: "0 auto", padding: 16 }, children: [_jsx("h2", { children: "MCP Server Discovery" }), allowCustomLists && (_jsxs("form", { onSubmit: handleAddCustom, style: { marginBottom: 16, display: "flex", gap: 8 }, children: [_jsx(Input, { type: "url", placeholder: "Add MCP server list URL", value: customUrl, onChange: (e) => setCustomUrl(e.target.value), required: true, style: { flex: 1 } }), _jsx(Button, { type: "submit", variant: "secondary", children: "Add" }), _jsx(Button, { type: "button", variant: "outline-danger", onClick: clearAll, children: "Clear" })] })), loading && (_jsxs("div", { style: { margin: "16px 0" }, children: [_jsx(Spinner, {}), " Loading server lists..."] })), error && (_jsx("div", { style: { margin: "16px 0" }, children: _jsx(Alert, { variant: "danger", children: error }) })), _jsx(ServerList, { servers: servers })] }) }));
};
export default CoreRoot;
//# sourceMappingURL=CoreRoot.js.map