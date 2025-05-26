// src/context/McpPoolContext.tsx
import { createContext, useContext, useMemo } from "react";
import { MCPClientPool } from "mcphappey-mcp";

const McpPoolContext = createContext<MCPClientPool | null>(null);

export const McpPoolProvider = ({ children }: { children: React.ReactNode }) => {
  const pool = useMemo(() => new MCPClientPool(), []);
  return <McpPoolContext.Provider value={pool}>{children}</McpPoolContext.Provider>;
};

export const useMcpPool = () => {
  const ctx = useContext(McpPoolContext);
  if (!ctx) throw new Error("useMcpPool must be used inside McpPoolProvider");
  return ctx;
};
