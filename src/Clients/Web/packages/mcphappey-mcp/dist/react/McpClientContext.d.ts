import { MCPClientPool } from "../clientPool";
interface McpClientContextValue {
    pool: MCPClientPool;
}
export declare const McpClientProvider: ({ children }: {
    children: React.ReactNode;
}) => import("react/jsx-runtime").JSX.Element;
export declare function useMcpClientContext(): McpClientContextValue;
export {};
//# sourceMappingURL=McpClientContext.d.ts.map