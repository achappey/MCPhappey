import {
  createContext,
  useContext,
  useState,
  ReactNode,
  useCallback,
  useEffect,
} from "react";
//import { createAiChatStore } from "mcphappeditor-ai";

// Interface for the chat metadata we manage outside ChatStore
interface ChatEntry {
  id: string;
  name: string;
}
/*
interface ChatManagerContextType {
  chatStore: ReturnType<typeof createAiChatStore>;
  chatEntries: Record<string, ChatEntry>;
  orderedChatIds: string[];
  selectedChatId: string | null;
  selectChat: (chatId: string | null) => void;
  createChat: (name?: string) => string;
  renameChat: (chatId: string, newName: string) => void;
  deleteChat: (chatId: string) => void;
  getChatName: (chatId: string) => string | undefined;
  getOrderedChats: () => ChatEntry[];
}

const ChatManagerContext = createContext<ChatManagerContextType | undefined>(
  undefined
);

export const ChatManagerProvider = ({ children }: { children: ReactNode }) => {
  const [chatStore] = useState(() => createAiChatStore());
  const [chatEntries, setChatEntries] = useState<Record<string, ChatEntry>>({});
  const [orderedChatIds, setOrderedChatIds] = useState<string[]>([]);
  const [selectedChatId, setSelectedChatId] = useState<string | null>(null);

  useEffect(() => {
    if (orderedChatIds.length === 0) {
      createChat("General Chat");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectChat = useCallback((chatId: string | null) => {
    setSelectedChatId(chatId);
  }, []);

  const createChat = useCallback(
    (name: string = "New Chat"): string => {
      const newChatId = crypto.randomUUID();
      const newEntry: ChatEntry = { id: newChatId, name };
      setChatEntries((prev) => ({ ...prev, [newChatId]: newEntry }));
      setOrderedChatIds((prev) => [newChatId, ...prev]);
      selectChat(newChatId);
      return newChatId;
    },
    [selectChat]
  );

  const renameChat = useCallback((chatId: string, newName: string) => {
    setChatEntries((prev) => {
      if (!prev[chatId]) return prev;
      return { ...prev, [chatId]: { ...prev[chatId], name: newName } };
    });
  }, []);

  const deleteChat = useCallback(
    (chatId: string) => {
      setChatEntries((prev) => {
        const { [chatId]: _, ...rest } = prev;
        return rest;
      });
      setOrderedChatIds((prev) => prev.filter((id) => id !== chatId));
      if (selectedChatId === chatId) {
        const newSelectedId = orderedChatIds.find((id) => id !== chatId);
        selectChat(newSelectedId || (orderedChatIds.length > 0 ? orderedChatIds[0] : null));
      }
    },
    [selectedChatId, selectChat, orderedChatIds]
  );

  const getChatName = useCallback(
    (chatId: string): string | undefined => chatEntries[chatId]?.name,
    [chatEntries]
  );

  const getOrderedChats = useCallback(
    (): ChatEntry[] => orderedChatIds.map((id) => chatEntries[id]).filter(Boolean),
    [orderedChatIds, chatEntries]
  );

  return (
    <ChatManagerContext.Provider
      value={{
        chatStore,
        chatEntries,
        orderedChatIds,
        selectedChatId,
        selectChat,
        createChat,
        renameChat,
        deleteChat,
        getChatName,
        getOrderedChats,
      }}
    >
      {children}
    </ChatManagerContext.Provider>
  );
};

export const useChatManager = (): ChatManagerContextType => {
  const context = useContext(ChatManagerContext);
  if (context === undefined) {
    throw new Error("useChatManager must be used within a ChatManagerProvider");
  }
  return context;
};
*/