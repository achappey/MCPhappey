import { jsx as _jsx, Fragment as _Fragment, jsxs as _jsxs } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
import { useState, useCallback } from "react";
import ToolCard from "./ToolCard";
import ToolModal from "./ToolModal";
import { useMcpConnect } from "../hooks/useMcpConnect";
const ToolsTab = ({ serverUrl, capabilities, tools, loading, error, }) => {
    const { Spinner, Alert } = useTheme();
    const { connect } = useMcpConnect();
    const [modalTool, setModalTool] = useState(null);
    const [submitError, setSubmitError] = useState(null);
    const [toolResult, setToolResult] = useState(null);
    const handleOpenTool = (tool) => {
        setModalTool(tool);
        setSubmitError(null);
        setToolResult(null);
    };
    const handleCloseModal = () => {
        setModalTool(null);
        setSubmitError(null);
        setToolResult(null);
    };
    const handleSubmitTool = useCallback(async (values) => {
        setSubmitError(null);
        try {
            const client = await connect(serverUrl);
            await new Promise((resolve) => setTimeout(resolve, 100));
            if (!modalTool)
                throw new Error("No tool selected");
            const result = await client.callTool({
                name: modalTool.name,
                arguments: values,
            });
            setToolResult(result);
            // eslint-disable-next-line no-console
            console.log("Tool result:", result);
        }
        catch (err) {
            setSubmitError(err?.message || String(err));
        }
    }, [connect, serverUrl, modalTool]);
    if (!capabilities?.tools)
        return _jsx("span", { children: "No tool support." });
    if (loading)
        return _jsx(Spinner, { size: "sm" });
    if (error)
        return _jsx(Alert, { variant: "danger", children: error });
    if (!tools || tools.length === 0)
        return _jsx("span", { children: "No tools found." });
    return (_jsxs(_Fragment, { children: [submitError && (_jsx(Alert, { variant: "danger", className: "mb-3", children: submitError })), _jsx("div", { style: {
                    display: "grid",
                    gap: 16,
                }, children: tools.map((t) => (_jsx("div", { children: _jsx(ToolCard, { tool: t, onOpen: handleOpenTool }) }, t.name))) }), _jsx(ToolModal, { show: !!modalTool, tool: modalTool, onClose: handleCloseModal, onSubmit: handleSubmitTool, result: toolResult })] }));
};
export default ToolsTab;
//# sourceMappingURL=ToolsTab.js.map