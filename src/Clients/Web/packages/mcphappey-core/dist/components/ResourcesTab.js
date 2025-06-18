import { jsx as _jsx, Fragment as _Fragment, jsxs as _jsxs } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
import { useState, useCallback } from "react";
import ResourceCard from "./ResourceCard";
import ResourceModal from "./ResourceModal";
import { useMcpConnect } from "../hooks/useMcpConnect";
const ResourcesTab = ({ serverUrl, capabilities, resources, loading, error, }) => {
    const { Spinner, Alert } = useTheme();
    const { connect } = useMcpConnect();
    const [modalResource, setModalResource] = useState(null);
    const [resourceContent, setResourceContent] = useState(null);
    const [reading, setReading] = useState(false);
    const [readError, setReadError] = useState(null);
    const handleReadResource = useCallback(async (res) => {
        setModalResource(res);
        setReading(true);
        setReadError(null);
        setResourceContent(null);
        try {
            const client = await connect(serverUrl);
            const data = await client.readResource({
                uri: res.uri,
            });
            setResourceContent(data);
        }
        catch (err) {
            setReadError(err?.message || String(err));
        }
        finally {
            setReading(false);
        }
    }, [connect, serverUrl]);
    const handleOpenLink = (res) => {
        window.open(res.uri, "_blank", "noopener");
    };
    const handleCloseModal = () => {
        setModalResource(null);
        setResourceContent(null);
        setReadError(null);
        setReading(false);
    };
    if (!capabilities?.resources)
        return _jsx("span", { children: "No resource support." });
    if (loading)
        return _jsx(Spinner, { size: "sm" });
    if (error)
        return _jsx(Alert, { variant: "danger", children: error });
    if (!resources || resources.length === 0)
        return _jsx("span", { children: "No resources found." });
    return (_jsxs(_Fragment, { children: [_jsx("div", { style: {
                    display: "grid",
                    gap: 16,
                }, children: resources.map((r) => (_jsx("div", { children: _jsx(ResourceCard, { resource: r, onRead: handleReadResource, onOpenLink: handleOpenLink }) }, r.id))) }), _jsx(ResourceModal, { show: !!modalResource, onClose: handleCloseModal, resourceName: modalResource?.name || "", mimeType: modalResource?.mimeType, content: resourceContent, loading: reading, error: readError })] }));
};
export default ResourcesTab;
//# sourceMappingURL=ResourcesTab.js.map