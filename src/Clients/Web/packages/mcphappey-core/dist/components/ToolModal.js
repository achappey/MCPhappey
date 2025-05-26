import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState, useEffect, useMemo } from "react";
import { useTheme } from "../ThemeContext";
import { JsonView, defaultStyles } from "react-json-view-lite";
import "react-json-view-lite/dist/index.css";
const ToolModal = ({ show, tool, onClose, onSubmit, result, }) => {
    const { Modal, Input, Button, Badge } = useTheme();
    const [formValues, setFormValues] = useState({});
    const [touched, setTouched] = useState({});
    const [view, setView] = useState("form");
    const schema = tool?.inputSchema ?? {};
    const properties = schema.properties ?? {};
    const requiredSet = useMemo(() => {
        const req = schema.required;
        return new Set(req ?? []);
    }, [schema.required]);
    // Reset when tool changes
    useEffect(() => {
        if (tool) {
            const initial = {};
            Object.entries(properties).forEach(([name, prop]) => {
                if (prop.default !== undefined)
                    initial[name] = prop.default;
                else if (prop.type === "boolean")
                    initial[name] = false;
                else
                    initial[name] = "";
            });
            setFormValues(initial);
            setTouched({});
        }
        setView("form");
    }, [tool, show]); // eslint-disable-line react-hooks/exhaustive-deps
    // Switch to result view when result arrives
    useEffect(() => {
        if (result !== undefined && result !== null)
            setView("result");
    }, [result]);
    if (!tool)
        return null;
    const handleChange = (name, value) => {
        setFormValues((v) => ({ ...v, [name]: value }));
        setTouched((t) => ({ ...t, [name]: true }));
    };
    const isValid = () => {
        for (const key of requiredSet) {
            const v = formValues[key];
            if (v === undefined ||
                v === null ||
                (typeof v === "string" && !v.trim()))
                return false;
        }
        return true;
    };
    const handleSubmit = (e) => {
        e.preventDefault();
        if (isValid()) {
            // Convert empty strings to null for enum properties that include null
            const fixed = {};
            Object.entries(properties).forEach(([name, prop]) => {
                let val = formValues[name];
                if (val === "" && prop.enum?.includes(null))
                    val = null;
                fixed[name] = val;
            });
            onSubmit(fixed);
        }
    };
    const allExpanded = () => true;
    const renderField = (name, prop) => {
        const required = requiredSet.has(name);
        const typeArray = Array.isArray(prop.type)
            ? prop.type
            : prop.type
                ? [prop.type]
                : [];
        if (prop.enum && prop.enum.length > 0) {
            return (_jsxs("select", { value: formValues[name] ?? "", onChange: (e) => handleChange(name, e.target.value), required: required, style: { padding: 6, width: "100%" }, children: [!required && !prop.enum.includes(null) && (_jsx("option", { value: "", children: "-- none --" })), prop.enum.map((opt) => (_jsx("option", { value: opt === null ? "" : String(opt), children: opt === null ? "-- none --" : String(opt) }, String(opt))))] }));
        }
        if (typeArray.includes("boolean")) {
            return (_jsx("input", { type: "checkbox", checked: !!formValues[name], onChange: (e) => handleChange(name, e.target.checked) }));
        }
        const inputType = typeArray.includes("number") ? "number" : "text";
        return (_jsx(Input, { type: inputType, required: required, value: formValues[name] ?? "", onChange: (e) => handleChange(name, e?.target?.value !== undefined ? e.target.value : e), placeholder: prop.description || name, style: {
                borderColor: touched[name] && required && !formValues[name] ? "#dc3545" : undefined,
            } }));
    };
    return (_jsx(Modal, { show: show, onHide: onClose, size: "lg", title: tool.name, children: _jsxs("div", { children: [_jsx("div", { style: { marginBottom: 12, color: "#666" }, children: tool.description }), view === "form" ? (_jsxs("form", { onSubmit: handleSubmit, children: [Object.keys(properties).length > 0 ? (_jsx("div", { style: { display: "flex", flexDirection: "column", gap: 16 }, children: Object.entries(properties).map(([name, prop]) => (_jsxs("div", { children: [_jsxs("label", { style: { fontWeight: 500 }, children: [name, requiredSet.has(name) && (_jsx("span", { style: { marginLeft: 6 }, children: _jsx(Badge, { bg: "danger", children: "required" }) }))] }), _jsx("div", { style: { color: "#666", fontSize: 13, marginBottom: 2 }, children: prop.description }), renderField(name, prop)] }, name))) })) : (_jsx("div", { style: { color: "#888", marginBottom: 16 }, children: "No inputs required for this tool." })), _jsxs("div", { style: { marginTop: 24, display: "flex", gap: 8 }, children: [_jsx(Button, { variant: "primary", type: "submit", disabled: !isValid(), children: "Call tool" }), _jsx(Button, { variant: "secondary", onClick: onClose, type: "button", children: "Close" })] })] })) : (_jsxs("div", { children: [_jsx("div", { style: { marginTop: 8 }, children: _jsx(JsonView, { data: result, shouldExpandNode: allExpanded, style: defaultStyles }) }), _jsxs("div", { style: { marginTop: 24, display: "flex", gap: 8 }, children: [_jsx(Button, { variant: "secondary", onClick: () => setView("form"), children: "Back" }), _jsx(Button, { variant: "primary", onClick: onClose, children: "Close" })] })] }))] }) }));
};
export default ToolModal;
//# sourceMappingURL=ToolModal.js.map