import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState, useEffect } from "react";
import { useTheme } from "../ThemeContext";
import { useServerPrimitives } from "../hooks/useServerPrimitives";
import ServerSummary from "./ServerSummary";
import PromptsTab from "./PromptsTab";
import ResourcesTab from "./ResourcesTab";
import ToolsTab from "./ToolsTab";
const ServerModal = ({ show, server, onClose }) => {
    const { Modal, Tabs, Tab, Button } = useTheme();
    const [tabKey, setTabKey] = useState("summary");
    const primitives = useServerPrimitives(server?.url || null);
    // Reset state on server/modal change
    useEffect(() => {
        setTabKey("summary");
    }, [server, show]);
    // Load all primitives when modal opens
    useEffect(() => {
        if (show && server) {
            primitives.loadAll();
        }
        // eslint-disable-next-line
    }, [show, server?.url]);
    if (!show || !server)
        return null;
    return (_jsx(Modal, { show: show, onHide: onClose, title: server.name, size: "lg", centered: true, children: _jsx("div", { className: "mcph-modal-body", style: { padding: 16 }, children: _jsxs(Tabs, { activeKey: tabKey, onSelect: setTabKey, className: "mb-3", children: [_jsx(Tab, { eventKey: "summary", title: "Summary", children: _jsx(ServerSummary, { server: server, loading: !!primitives.loading, toolCount: primitives.tools?.length, resourceCount: primitives.resources?.length, promptCount: primitives.prompts?.length, error: primitives.error, capabilities: primitives.capabilities }) }), _jsx(Tab, { eventKey: "prompts", title: "Prompts", children: _jsx(PromptsTab, { serverUrl: server.url, capabilities: primitives.capabilities, prompts: primitives.prompts, loading: !!primitives.loading, error: primitives.error }) }), _jsx(Tab, { eventKey: "resources", title: "Resources", children: _jsx(ResourcesTab, { serverUrl: server.url, capabilities: primitives.capabilities, resources: primitives.resources, loading: !!primitives.loading, error: primitives.error }) }), _jsx(Tab, { eventKey: "tools", title: "Tools", children: _jsx(ToolsTab, { serverUrl: server.url, capabilities: primitives.capabilities, tools: primitives.tools, loading: !!primitives.loading, error: primitives.error }) })] }) }) }));
};
export default ServerModal;
//# sourceMappingURL=ServerModal.js.map