import { connectSimple, CreateMessageRequest, 
  CreateMessageResult, McpConnectResult } from "mcphappeditor-mcp";
import type { StateCreator } from "zustand";

export type UiAttachment = {
  id: string;
  name: string;
  size: number;
  type: string;
  file: File;
};

export type UiSlice = {
  showActivities: boolean;
  toggleActivities: () => void;
  setActivities: (open: boolean) => void;

  sidebarOpen: boolean;
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;

  conversationStorage: "local" | "remote";
  setConversationStorage: (kind: "local" | "remote") => void;

  remoteStorageConnected: boolean;
  setRemoteStorageConnected: (connected: boolean) => void;

  attachments: UiAttachment[];
  addAttachment: (file: File) => void;
  removeAttachment: (id: string) => void;
  clearAttachments: () => void;

  chatAppMcp?: McpConnectResult["client"];
  connectChatApp: (
    clientName: string,
    clientVersion: string,
    url: string,
    opts: McpConnectOpts
  ) => Promise<void>;
};

type McpConnectOpts = {
  onSample?: (server: string, req: CreateMessageRequest) => Promise<CreateMessageResult>;
};

export const createUiSlice: StateCreator<
  any,
  [],
  [],
  UiSlice
> = (set, get, store) => ({
  showActivities: false,
  chatAppMcp: undefined,
  connectChatApp: async (clientName: string, clientVersion: string, url: string, opts: McpConnectOpts) => {
    const client = await connectSimple(url, "http", opts);

    set((state: any) => ({
      chatAppMcp: client,
    }));
  },
  getConversationName: async (messages: any[]) => {
    const { chatAppMcp } = get();
    //chatAppMcp.callTool.("");
    set((state: any) => ({
      // chatAppMcp: client,
    }));
  },
  toggleActivities: () =>
    set((s: any) => ({
      showActivities: !s.showActivities,
    })),
  setActivities: (open: boolean) =>
    set(() => ({
      showActivities: open,
    })),
  sidebarOpen: true,
  toggleSidebar: () =>
    set((s: any) => ({
      sidebarOpen: !s.sidebarOpen,
    })),
  setSidebarOpen: (open: boolean) =>
    set(() => ({
      sidebarOpen: open,
    })),
  conversationStorage: "local",
  setConversationStorage: (kind: "local" | "remote") =>
    set(() => ({
      conversationStorage: kind,
    })),
  remoteStorageConnected: false,
  setRemoteStorageConnected: (connected: boolean) =>
    set(() => ({
      remoteStorageConnected: connected,
    })),
  attachments: [],
  addAttachment: (file: File) =>
    set((s: any) => ({
      attachments: [
        ...s.attachments,
        {
          id: crypto.randomUUID(),
          name: file.name,
          size: file.size,
          type: file.type,
          file,
        },
      ],
    })),
  removeAttachment: (id: string) =>
    set((s: any) => ({
      attachments: s.attachments.filter((a: any) => a.id !== id),
    })),
  clearAttachments: () =>
    set(() => ({
      attachments: [],
    })),
});
