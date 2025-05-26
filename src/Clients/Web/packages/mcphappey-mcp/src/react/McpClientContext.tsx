// McpClientContext.tsx (React context for MCP client pool)
import { createContext, useContext, useMemo, useRef } from "react";
import { MCPClientPool } from "../clientPool";

interface McpClientContextValue {
  pool: MCPClientPool;
}

const McpClientContext = createContext<McpClientContextValue | undefined>(undefined);

export const McpClientProvider = ({ children }: { children: React.ReactNode }) => {
  // Singleton pool instance for the app
  const poolRef = useRef(new MCPClientPool());
  const value = useMemo(() => ({ pool: poolRef.current }), []);
  return (
    <McpClientContext.Provider value={value}>
      {children}
    </McpClientContext.Provider>
  );
};

export function useMcpClientContext() {
  const ctx = useContext(McpClientContext);
  if (!ctx) throw new Error("useMcpClientContext must be used within a McpClientProvider");
  return ctx;
}
