export declare const useMcpConnect: () => {
    connect: (url: string) => Promise<import("@modelcontextprotocol/sdk/client").Client<{
        method: string;
        params?: {
            [x: string]: unknown;
            _meta?: {
                [x: string]: unknown;
                progressToken?: string | number | undefined;
            } | undefined;
        } | undefined;
    }, {
        method: string;
        params?: {
            [x: string]: unknown;
            _meta?: {
                [x: string]: unknown;
            } | undefined;
        } | undefined;
    }, {
        [x: string]: unknown;
        _meta?: {
            [x: string]: unknown;
        } | undefined;
    }>>;
    pool: import("mcphappey-mcp").MCPClientPool;
};
//# sourceMappingURL=useMcpConnect.d.ts.map