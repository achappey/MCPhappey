import { UnifiedServersSlice } from "./unifiedServersSlice";
export declare const useAppStore: import("zustand").UseBoundStore<Omit<import("zustand").StoreApi<UnifiedServersSlice>, "persist"> & {
    persist: {
        setOptions: (options: Partial<import("zustand/middleware").PersistOptions<UnifiedServersSlice, {
            servers: import("mcphappey-types").McpServerWithName[];
        }>>) => void;
        clearStorage: () => void;
        rehydrate: () => Promise<void> | void;
        hasHydrated: () => boolean;
        onHydrate: (fn: (state: UnifiedServersSlice) => void) => () => void;
        onFinishHydration: (fn: (state: UnifiedServersSlice) => void) => () => void;
        getOptions: () => Partial<import("zustand/middleware").PersistOptions<UnifiedServersSlice, {
            servers: import("mcphappey-types").McpServerWithName[];
        }>>;
    };
}>;
//# sourceMappingURL=store.d.ts.map