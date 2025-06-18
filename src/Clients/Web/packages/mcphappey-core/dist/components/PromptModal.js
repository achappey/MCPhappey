import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState, useEffect } from "react";
import { useTheme } from "../ThemeContext";
import { JsonView, defaultStyles } from "react-json-view-lite";
import "react-json-view-lite/dist/index.css";
const PromptModal = ({ show, prompt, onClose, onSubmit, result, }) => {
    const { Modal, Input, Button, Badge } = useTheme();
    const [formValues, setFormValues] = useState({});
    const [touched, setTouched] = useState({});
    const [view, setView] = useState("form");
    useEffect(() => {
        if (prompt?.arguments) {
            // Reset form values when prompt changes
            const initial = {};
            prompt.arguments.forEach((a) => {
                initial[a.name] = "";
            });
            setFormValues(initial);
            setTouched({});
        }
        setView("form");
    }, [prompt, show]);
    // Switch to result view when result is set
    useEffect(() => {
        if (result !== undefined && result !== null) {
            setView("result");
        }
    }, [result]);
    if (!prompt)
        return null;
    const handleChange = (name, value) => {
        setFormValues((v) => ({ ...v, [name]: value }));
        setTouched((t) => ({ ...t, [name]: true }));
    };
    const requiredArgs = (prompt.arguments || []).filter((a) => a.required);
    const isValid = requiredArgs.length === 0 ||
        requiredArgs.every((a) => !!formValues[a.name]?.trim());
    const handleSubmit = (e) => {
        e.preventDefault();
        if (isValid) {
            onSubmit(formValues);
        }
    };
    const allExpanded = () => true;
    return (_jsx(Modal, { show: show, onHide: onClose, size: "lg", title: prompt.name, children: _jsxs("div", { children: [_jsx("div", { style: { marginBottom: 12, color: "#666" }, children: prompt.description }), view === "form" ? (_jsxs("form", { onSubmit: handleSubmit, children: [prompt.arguments && prompt.arguments.length > 0 ? (_jsx("div", { style: { display: "flex", flexDirection: "column", gap: 16 }, children: prompt.arguments.map((arg) => (_jsxs("div", { children: [_jsxs("label", { style: { fontWeight: 500 }, children: [arg.name, arg.required && (_jsx("span", { style: { marginLeft: 6 }, children: _jsx(Badge, { bg: "danger", children: "required" }) }))] }), _jsx("div", { style: { color: "#666", fontSize: 13, marginBottom: 2 }, children: arg.description }), _jsx(Input, { type: "text", required: arg.required, value: formValues[arg.name] || "", onChange: (e) => handleChange(arg.name, e?.target?.value !== undefined ? e.target.value : e), placeholder: arg.description || arg.name, style: {
                                            borderColor: touched[arg.name] &&
                                                arg.required &&
                                                !formValues[arg.name]
                                                ? "#dc3545"
                                                : undefined,
                                        } })] }, arg.name))) })) : (_jsx("div", { style: { color: "#888", marginBottom: 16 }, children: "No arguments required for this prompt." })), _jsxs("div", { style: { marginTop: 24, display: "flex", gap: 8 }, children: [_jsx(Button, { variant: "primary", type: "submit", disabled: !isValid, children: "Get prompt" }), _jsx(Button, { variant: "secondary", onClick: onClose, type: "button", children: "Close" })] })] })) : (_jsxs("div", { children: [_jsx("div", { style: { marginTop: 8 }, children: _jsx(JsonView, { data: result, shouldExpandNode: allExpanded, style: defaultStyles }) }), _jsxs("div", { style: { marginTop: 24, display: "flex", gap: 8 }, children: [_jsx(Button, { variant: "secondary", onClick: () => setView("form"), children: "Back" }), _jsx(Button, { variant: "primary", onClick: onClose, children: "Close" })] })] }))] }) }));
};
export default PromptModal;
//# sourceMappingURL=PromptModal.js.map