import { jsx as _jsx, Fragment as _Fragment, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from "react";
import { useTheme } from "../ThemeContext";
import ServerCard from "./ServerCard";
import ServerModal from "./ServerModal";
import { getAccessToken } from "mcphappey-auth";
const ServerList = ({ servers }) => {
    const { Alert, Switch, TextArea } = useTheme();
    const [selectedServer, setSelectedServer] = useState(null);
    const [modalOpen, setModalOpen] = useState(false);
    // --- ADDED: toggle state
    const [showRaw, setShowRaw] = useState(false);
    const handleShowDetails = (server) => {
        setSelectedServer(server);
        setModalOpen(true);
    };
    const handleCloseModal = () => {
        setModalOpen(false);
        setSelectedServer(null);
    };
    // --- ADDED: augment servers with auth header if token present
    const serversWithAuth = servers.map((s) => {
        const token = getAccessToken(s.url);
        return token
            ? {
                ...s,
                headers: { ...(s.headers ?? {}), Authorization: `Bearer ${token}` },
            }
            : s;
    });
    const serverDict = Object.fromEntries(serversWithAuth.map(({ name, ...rest }) => [name, rest]));
    if (servers.length === 0) {
        return (_jsx(Alert, { variant: "info", className: "my-3", children: "No MCP servers found." }));
    }
    return (_jsxs(_Fragment, { children: [_jsx("div", { style: { marginBottom: 16 }, children: _jsx(Switch, { id: "raw-json-toggle", label: "Show raw JSON", checked: showRaw, onChange: setShowRaw }) }), showRaw ? (_jsx(TextArea, { rows: Math.min(20, serversWithAuth.length * 4), value: JSON.stringify({ servers: serverDict }, null, 2), style: {
                    fontFamily: "monospace",
                    fontSize: 12,
                    background: "#f8f9fa",
                    borderRadius: 4,
                }, onChange: () => { } })) : (_jsxs(_Fragment, { children: [_jsx("div", { style: { display: "flex", flexWrap: "wrap", gap: 16 }, children: servers.map((server) => (_jsx("div", { style: { flex: "1 0 250px", minWidth: 250 }, children: _jsx(ServerCard, { server: server, onShowDetails: handleShowDetails }) }, server.name))) }), _jsx(ServerModal, { show: modalOpen, server: selectedServer, onClose: handleCloseModal })] }))] }));
};
export default ServerList;
//# sourceMappingURL=ServerList.js.map