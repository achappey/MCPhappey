import { create } from "zustand";
import { persist } from "zustand/middleware";
import { createUnifiedServersSlice } from "./unifiedServersSlice";
export const useAppStore = create()(persist((...a) => createUnifiedServersSlice(...a), {
    name: "mcp_servers", // localStorage key
    partialize: (state) => ({ servers: state.servers }),
}));
//# sourceMappingURL=store.js.map