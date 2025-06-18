import { jsx as _jsx } from "react/jsx-runtime";
// src/context/McpPoolContext.tsx
import { createContext, useContext, useMemo } from "react";
import { MCPClientPool } from "mcphappey-mcp";
const McpPoolContext = createContext(null);
export const McpPoolProvider = ({ children }) => {
    const pool = useMemo(() => new MCPClientPool(), []);
    return _jsx(McpPoolContext.Provider, { value: pool, children: children });
};
export const useMcpPool = () => {
    const ctx = useContext(McpPoolContext);
    if (!ctx)
        throw new Error("useMcpPool must be used inside McpPoolProvider");
    return ctx;
};
//# sourceMappingURL=McpPoolContext.js.map