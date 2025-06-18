import { StateCreator } from "zustand";
import { McpServerWithName } from "mcphappey-types";
export type UnifiedServersSlice = {
    servers: McpServerWithName[];
    loading: boolean;
    error: string | null;
    importList: (url: string) => Promise<void>;
    clearAll: () => void;
};
export declare const createUnifiedServersSlice: StateCreator<UnifiedServersSlice, [
], [
], UnifiedServersSlice>;
//# sourceMappingURL=unifiedServersSlice.d.ts.map