import { jsx as _jsx } from "react/jsx-runtime";
// McpClientContext.tsx (React context for MCP client pool)
import { createContext, useContext, useMemo, useRef } from "react";
import { MCPClientPool } from "../clientPool";
const McpClientContext = createContext(undefined);
export const McpClientProvider = ({ children }) => {
    // Singleton pool instance for the app
    const poolRef = useRef(new MCPClientPool());
    const value = useMemo(() => ({ pool: poolRef.current }), []);
    return (_jsx(McpClientContext.Provider, { value: value, children: children }));
};
export function useMcpClientContext() {
    const ctx = useContext(McpClientContext);
    if (!ctx)
        throw new Error("useMcpClientContext must be used within a McpClientProvider");
    return ctx;
}
//# sourceMappingURL=McpClientContext.js.map