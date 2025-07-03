// Core chat provider for mcphappeditor-core, using mcphappeditor-ai abstraction only

import { AiChatProvider } from "mcphappeditor-ai";
import { useMemo, useRef } from "react";
import { ChatContext } from "./ChatContext";
import type { ReactNode, FC } from "react";
import { useAppStore } from "mcphappeditor-state";
import { useConversations } from "mcphappeditor-conversations";
import type { AiChatConfig } from "mcphappeditor-ai";
import { useParams, useNavigate } from "react-router";
import { store } from "mcphappeditor-state";

export interface ChatConfig extends AiChatConfig {
  modelsApi?: string;
  samplingApi?: string;
  appName?: string;
  appVersion?: string;
}

export const ChatProvider: FC<{ config: ChatConfig; children: ReactNode }> = ({
  config,
  children,
}) => {
  const callTool = useAppStore((a) => a.callTool);
  const { addMessage, updateMessage, get, rename } = useConversations();
  const chatAppMcp = useAppStore((s) => s.chatAppMcp);
  const { conversationId } = useParams<{ conversationId?: string }>();
  const maxSteps = useAppStore((s) => s.maxSteps);
  const addChatError = useAppStore((s) => s.addChatError);

  // --- Tool call cancellation logic ---
  const toolAbortRef = useRef<AbortController | null>(null);

  // Expose stopTool for UI to call (e.g. from ChatPanel)
  const stopTool = () => {
    toolAbortRef.current?.abort();
  };

  // Enhanced onToolCall: create AbortController, pass signal to callTool
  const onToolCall = async ({ toolCall }: any) => {
    toolAbortRef.current = new AbortController();
    try {
      return await callTool(
        toolCall.toolCallId,
        toolCall.toolName,
        toolCall.input,
        toolAbortRef.current.signal
      );
    } finally {
      // Optionally clear after finish/cancel
      // toolAbortRef.current = null;
    }
  };

  // Memoize an auth-aware fetch if needed
  const authFetch = useMemo(() => {
    if (!config.getAccessToken && !config.headers && !config.fetch)
      return undefined;
    return async (input: RequestInfo | URL, init?: RequestInit) => {
      let headers: Record<string, string> = { ...(config.headers ?? {}) };
      if (config.getAccessToken) {
        try {
          const token = await config.getAccessToken();
          headers.Authorization = `Bearer ${token}`;
        } catch {}
      }
      if (init?.headers) {
        headers = { ...headers, ...(init.headers as Record<string, string>) };
      }
      return (config.fetch ?? fetch)(input, { ...init, headers });
    };
  }, [config]);

  // Add stopTool to context value for ChatPanel to consume
  const value = useMemo(
    () => ({
      config,
      stopTool,
    }),
    [config]
  );

  const navigate = useNavigate();
  const handleBeforeSend = () => {
    const targetId =
      store.getState().selectedConversationId ?? conversationId ?? "";
    if (!conversationId && targetId) {
      navigate("/" + targetId, { replace: true });
    }
    activeRequestConversationRef.current = targetId;
  };

  // Track pending user message id -> conversation id
  const pendingRef = useRef<Record<string, string>>({});
  if (typeof window !== "undefined") {
    (window as any).ChatProvider_pendingRef = pendingRef;
  }

  const activeRequestConversationRef = useRef<string | null>(null);
  const handleFinish = async ({ message }: any) => {
    if (message.role !== "assistant") return;
    // Use the conversation ID that was active when the request started
    const targetConversationId = activeRequestConversationRef.current;

    if (targetConversationId) {
      try {
        await updateMessage(targetConversationId, message.id, {
          ...message,
        });
      } catch (error) {
        await addMessage(targetConversationId, {
          ...message,
        });
      }

      // --- Conversation naming logic ---
     try {
       const mcpClient = chatAppMcp ?? store.getState().chatAppMcp;
       if (!mcpClient) return;
       const conv = await get(targetConversationId);
        const name = conv?.metadata?.name ?? "";
        if (
          typeof name === "string" &&
          name.trim().toLowerCase() === "new chat" &&
          Array.isArray(conv?.messages)
        ) {
          const assistantCount = conv.messages.filter(
            (m: any) => m.role === "assistant"
          ).length;
          if (assistantCount === 1) {
            const firstUser = conv.messages.find((m: any) => m.role === "user");
            if (firstUser) {
              let userMsg = "";
              if (Array.isArray(firstUser.parts)) {
                userMsg = firstUser.parts
                  .filter(
                    (part) =>
                      part.type === "text" && typeof part.text === "string"
                  )
                  .map((part) => part.text)
                  .join("\n");
              }

              if (userMsg) {
              const res = await mcpClient.callTool({
                  name: "ChatApp_ExecuteGenerateConversationName",
                  arguments: { userMessage: userMsg },
                });
                let title = "";
                if (
                  res &&
                  Array.isArray(res.content) &&
                  res.content.length > 0 &&
                  typeof res.content[0]?.text === "string"
                ) {
                  title = res.content[0].text;
                }
                if (title && typeof title === "string") {
                  await rename(targetConversationId, title);
                }
              }
            }
          }
        }
      } catch (err) {
        // Only log for developer debugging, not user
        // eslint-disable-next-line no-console
        console.warn("Conversation naming failed", err);
      }
    } else {
      // eslint-disable-next-line no-console
      console.warn("No conversation ID found for assistant message:", message);
    }
  }
  //, [chatAppMcp]);

  return (
    <ChatContext.Provider value={value}>
      <AiChatProvider
        onToolCall={onToolCall}
        onFinish={handleFinish}
        onError={addChatError}
        maxSteps={maxSteps}
        api={config.api}
        onBeforeSend={handleBeforeSend}
        fetch={authFetch}
      >
        {children}
      </AiChatProvider>
    </ChatContext.Provider>
  );
};
