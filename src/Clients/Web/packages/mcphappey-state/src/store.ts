import { create } from "zustand";
import { persist } from "zustand/middleware";
import { createUnifiedServersSlice, UnifiedServersSlice } from "./unifiedServersSlice";

export const useAppStore = create<UnifiedServersSlice>()(
  persist(
    (...a) => createUnifiedServersSlice(...a),
    {
      name: "mcp_servers", // localStorage key
      partialize: (state) => ({ servers: state.servers }),
    }
  )
);
