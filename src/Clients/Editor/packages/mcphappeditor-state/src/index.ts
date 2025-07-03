import { createAppStore, RootState } from "./createAppStore";
import { useStore } from "zustand/react";

const store = createAppStore();
/** Generic selector hook for the global store. */
const useAppStore = <T>(selector: (state: RootState) => T): T =>
  useStore(store, selector);

/** Selector for remoteStorageConnected flag */
export const useRemoteStorageConnected = () =>
  useAppStore(s => (s as any).remoteStorageConnected as boolean);

export { createAppStore, useAppStore, store };

export type { Resource, ResourceTemplate } from "mcphappeditor-mcp";

export { SamplingRequest } from "./slices/mcpSlice";
