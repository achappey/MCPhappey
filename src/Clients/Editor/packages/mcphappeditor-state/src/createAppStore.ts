import { createStore } from "zustand";
import { createServersSlice, type ServersSlice } from "./slices/serversSlice";
import { createChatSlice, type ChatSlice } from "./slices/chatSlice";
import { createMcpSlice, type McpSlice } from "./slices/mcpSlice";
import { createUiSlice, type UiSlice } from "./slices/uiSlice";
import { withPersist } from "./persist";

export type RootState = ServersSlice & ChatSlice & McpSlice & UiSlice;

export const createAppStore = () =>
  createStore<RootState, [["zustand/persist", unknown]]>(
    withPersist(
      (set, get, store) => ({
        ...createServersSlice(set, get, store),
        ...createChatSlice(set, get, store),
        ...createMcpSlice(set, get, store),
        ...createUiSlice(set, get, store),
      })
    )
  );
