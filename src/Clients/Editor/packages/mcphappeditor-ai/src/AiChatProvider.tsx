import React, { createContext, useContext, useMemo } from "react";
import { useChat } from "@ai-sdk/react";
import { DefaultChatTransport, TextStreamChatTransport  } from "ai";

// Types for the context value
interface AiChatContextValue {
  messages: any[];
  sendMessage: (msg: any, opts?: any) => Promise<void>;
  status: "submitted" | "streaming" | "ready" | "error";
  setMessages: any;
  stop: () => Promise<void>;
}

interface AiChatProviderProps {
  children: React.ReactNode;
  conversationId?: string;
  api?: string;
  maxSteps?: number;
  fetch?: typeof fetch;
  onToolCall?: ({ toolCall }: any) => Promise<any>;
  onFinish?: ({ message }: any) => void;
  onError?: (error: Error) => void;
  onBeforeSend?: () => void;
}

const AiChatContext = createContext<AiChatContextValue | undefined>(undefined);

export const AiChatProvider: React.FC<AiChatProviderProps> = ({
  children,
  conversationId,
  api,
  maxSteps,
  onToolCall,
  onError,
  onBeforeSend,
  fetch,
  onFinish,
}) => {
  const transport = useMemo(
    () =>
      new DefaultChatTransport({
        api, // your backend URL
        fetch, // optional custom fetch with auth
      }),
    [api, fetch]
  );

  // Use the ai-sdk hook
  const { messages, sendMessage, status, setMessages, stop } = useChat({
    id: conversationId,
    maxSteps,
    transport,
    onFinish,
    onError,
    onToolCall,
  });

  console.log(messages);

  const originalSendMessage = sendMessage;
  const wrappedSendMessage = React.useCallback(
    async (msg: any, opts?: any) => {
      // Call the hook before sending
      if (onBeforeSend) {
        onBeforeSend();
      }
      return originalSendMessage(msg, opts);
    },
    [originalSendMessage, onBeforeSend]
  );

  const value = useMemo(
    () => ({
      messages,
      sendMessage: wrappedSendMessage,
      setMessages,
      status,
      stop,
    }),
    [messages, wrappedSendMessage, status, setMessages, stop]
  );

  return (
    <AiChatContext.Provider value={value}>{children}</AiChatContext.Provider>
  );
};

// Granular hooks
export const useAiSetMessages = () => {
  const ctx = useContext(AiChatContext);
  if (!ctx)
    throw new Error("useAiChatMessages must be used within AiChatProvider");
  return ctx.setMessages;
};

// Granular hooks
export const useAiChatMessages = () => {
  const ctx = useContext(AiChatContext);
  if (!ctx)
    throw new Error("useAiChatMessages must be used within AiChatProvider");
  return ctx.messages;
};

export const useAiChatAppend = () => {
  const ctx = useContext(AiChatContext);
  if (!ctx)
    throw new Error("useAiChatAppend must be used within AiChatProvider");
  return ctx.sendMessage;
};

export const useAiChatStatus = () => {
  const ctx = useContext(AiChatContext);
  if (!ctx)
    throw new Error("useAiChatStatus must be used within AiChatProvider");
  return ctx.status;
};

export const useAiChatStop = () => {
  const ctx = useContext(AiChatContext);
  if (!ctx) throw new Error("useAiChatStop must be used within AiChatProvider");
  return ctx.stop;
};
