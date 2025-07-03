import { connectMcpServer, CreateMessageRequest, CreateMessageResult, McpConnectResult } from "mcphappeditor-mcp";
import type { StateCreator } from "zustand";

export type ChatSlice = {
  selectedConversationId: string | null;
  selectConversation: (id: string | null) => void;
  maxSteps?: number
  temperature?: number
  systemInstructions?: string
  chatErrors?: string[]
 
  addChatError: (error: Error) => void
  dismissChatError: (error: string) => void
};


export const createChatSlice: StateCreator<
  any,
  [],
  [],
  ChatSlice
> = (set, get) => ({
  selectedConversationId: null,
  maxSteps: 25,
  temperature: 1,
  chatErrors: [],
  addChatError: (error) => {
    const message = typeof error === "string" ? error : error.message;
    set((state: any) => ({
      chatErrors: [...state.chatErrors, message],
    }));
  },
  dismissChatError: (error) => {
    set((state: any) => ({
      chatErrors: state.chatErrors.filter((e: any) => e !== error),
    }));
  },
  selectConversation: (id) =>
    set(() => ({
      selectedConversationId: id,
    })),
});
