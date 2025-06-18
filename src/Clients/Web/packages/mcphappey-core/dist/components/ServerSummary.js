import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
import PrettyJson from "./PrettyJson";
const ServerSummary = ({ server, loading, promptCount, resourceCount, toolCount, error, capabilities, }) => {
    const { Table, Badge, Spinner, Alert } = useTheme();
    return (_jsxs(_Fragment, { children: [_jsx(Table, { borderless: true, size: "sm", children: _jsxs("tbody", { children: [_jsxs("tr", { children: [_jsx("th", { children: "Type" }), _jsx("td", { children: server.type })] }), _jsxs("tr", { children: [_jsx("th", { children: "URL" }), _jsx("td", { style: { wordBreak: "break-all" }, children: server.url })] }), server.headers && (_jsxs("tr", { children: [_jsx("th", { children: "Headers" }), _jsx("td", { children: _jsx(PrettyJson, { data: server.headers }) })] })), Object.entries(server)
                            .filter(([key]) => !["name", "type", "url", "headers"].includes(key))
                            .map(([key, value]) => (_jsxs("tr", { children: [_jsx("th", { children: key }), _jsx("td", { children: typeof value === "object" ? (_jsx(PrettyJson, { data: value })) : (String(value)) })] }, key)))] }) }), _jsx("hr", {}), _jsx("h6", { children: "Capabilities" }), loading ? (_jsx(Spinner, { size: "sm" })) : error ? (_jsx(Alert, { variant: "danger", children: error })) : capabilities ? (_jsxs("div", { style: { display: "flex", gap: 12 }, children: [capabilities.prompts && (_jsxs(Badge, { bg: "primary", children: [promptCount, " prompt(s)"] })), capabilities.resources && (_jsxs(Badge, { bg: "success", children: [resourceCount, " resource(s)"] })), capabilities.tools && (_jsxs(Badge, { bg: "warning", children: [toolCount, " tool(s)"] })), !capabilities.prompts &&
                        !capabilities.resources &&
                        !capabilities.tools && _jsx("span", { children: "No primitives supported" })] })) : (_jsx("span", { children: "\u2014" }))] }));
};
export default ServerSummary;
//# sourceMappingURL=ServerSummary.js.map