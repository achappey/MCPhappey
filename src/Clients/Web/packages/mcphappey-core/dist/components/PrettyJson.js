import { jsx as _jsx } from "react/jsx-runtime";
/**
 * Pretty prints a JSON object as a <pre> block.
 */
const PrettyJson = ({ data }) => (_jsx("pre", { className: "mcph-pretty-json", style: { fontSize: 12, background: "#f8f9fa", padding: 8, borderRadius: 4, overflowX: "auto" }, children: JSON.stringify(data, null, 2) }));
export default PrettyJson;
//# sourceMappingURL=PrettyJson.js.map