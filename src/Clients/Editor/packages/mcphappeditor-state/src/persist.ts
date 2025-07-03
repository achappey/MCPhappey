import { persist } from "zustand/middleware";
import type { StateCreator } from "zustand";

type PersistMutators = [["zustand/persist", unknown]];
import type { ServersSlice } from "./slices/serversSlice";
import type { ChatSlice } from "./slices/chatSlice";
import { McpSlice } from "./slices/mcpSlice";
import { UiSlice } from "./slices/uiSlice";

type RootState = ServersSlice & ChatSlice & McpSlice & UiSlice;

export const withPersist = (
  creator: StateCreator<RootState, PersistMutators, [], RootState>
) =>
  persist(creator, {
    name: "aihappey_store_v5",
    partialize: (s) => ({
      endpoints: s.endpoints,
      servers: s.servers,
      selected: s.selected,
      conversationStorage: s.conversationStorage,
      remoteStorageConnected: s.remoteStorageConnected,
      logLevel: s.logLevel,
    }),
    migrate: (persistedState, version) => {
      // On version bump, reset endpoints, servers, and selected
      if (version < 5) {
        const safeState = typeof persistedState === "object" && persistedState !== null ? persistedState : {};
        return {
          ...safeState,

          endpoints: [],
          servers: {},
          selected: [],
        };
      }
      return persistedState;
    },
  });
