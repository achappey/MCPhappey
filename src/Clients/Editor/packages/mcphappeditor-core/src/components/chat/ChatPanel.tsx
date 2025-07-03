import { useAppStore } from "mcphappeditor-state";
import { useImperativeHandle, forwardRef, useEffect, useMemo } from "react";
import {
  useAiChatStatus,
  useAiSetMessages,
  useAiChatMessages,
  useAiChatStop,
} from "mcphappeditor-ai";
import { MessageInput } from "./MessageInput";
import { MessageList } from "./MessageList";
import { DisclaimerBar } from "./DisclaimerBar";
import { useTheme } from "../../ThemeContext";
import { useConversations } from "mcphappeditor-conversations";
import { useTranslation } from "mcphappeditor-i18n";
import { useSendMessage } from "./hooks/useSendMessage";
import { useChatContext } from "./ChatContext";
import { useChatFileDrop } from "./hooks/useChatFileDrop";
import { ChatErrors } from "./ChatErrors";
import { useSystemMessage } from "./hooks/useSystemMessage";
import { WelcomeMessage } from "./WelcomeMessage";

export interface ChatPanelHandle {
  send: (content: string) => Promise<void>;
}

interface ChatPanelProps {
  model?: string;
  hideInput?: boolean;
  showCitations: (items: any[]) => void;
  showToolsDrawer?: (tools: any[]) => void;
}

export const ChatPanel = forwardRef<ChatPanelHandle, ChatPanelProps>(
  ({ model, hideInput, showCitations, showToolsDrawer }, ref) => {
    const { send } = useSendMessage({ model });
    const status = useAiChatStatus();
    const setMessages = useAiSetMessages();
    const stop = useAiChatStop();
    // Get stopTool from context (for tool cancellation)
    const { stopTool } = useChatContext();
    const { Spinner } = useTheme();
    const selectedConversationId = useAppStore((s) => s.selectedConversationId);
    const conversations = useConversations();
    const currentMessages = useAiChatMessages();
    const pending = status === "submitted" || status === "streaming";
    const addAttachment = useAppStore((s) => s.addAttachment);
    const { t } = useTranslation();
    const { isOver, dropRef, handleDrop, handleDragOver } =
      useChatFileDrop(addAttachment);

    useImperativeHandle(ref, () => ({
      send,
    }));

    // Memoize system message so it only rebuilds when its deps change
    const systemMsg = useSystemMessage();

    // 👇 This handles fetching/merging messages when the ID changes.
    // useConversationMessages(selectedConversationId);
    // Effect 1: Fetch conversation ONLY when selectedConversationId changes
    useEffect(() => {
      if (!selectedConversationId) {
        setMessages([systemMsg]);
        return;
      }
      const load = async (cid: string) => {
        const conv = await conversations.get(cid);
        const convMsgs = conv?.messages ? [...conv.messages] : [];
        convMsgs.sort(
          (a, b) =>
            new Date(a.metadata?.timestamp).getTime() -
            new Date(b.metadata?.timestamp).getTime()
        );

        // Find pending user message for this conversation (if any)
        let pendingUserMsg = null;
        if (
          typeof window !== "undefined" &&
          (window as any).ChatProvider_pendingRef
        ) {
          const pendingRef = (window as any).ChatProvider_pendingRef.current;
          const pendingMsgId = Object.keys(pendingRef).find(
            (msgId) => pendingRef[msgId] === cid
          );
          if (pendingMsgId) {
            // Try to find the actual message in convMsgs
            pendingUserMsg = convMsgs.find(
              (m) => m.id === pendingMsgId && m.role === "user"
            );
            // If not found, try to get it from a temporary store (if implemented)
            if (
              !pendingUserMsg &&
              (window as any).ChatProvider_pendingMessages
            ) {
              pendingUserMsg =
                (window as any).ChatProvider_pendingMessages[pendingMsgId] ||
                null;
            }
            // Remove from pendingRef after merging
            delete pendingRef[pendingMsgId];
          }
        }

        // Only keep one system message (the freshly rebuilt one)
        const nonSystem = convMsgs.filter((m) => m.role !== "system");
        // If pendingUserMsg exists and not in convMsgs, add it
        const hasPending =
          pendingUserMsg && !nonSystem.some((m) => m.id === pendingUserMsg.id);
        const merged = [
          systemMsg,
          ...(hasPending ? [pendingUserMsg] : []),
          ...nonSystem,
        ];

        setMessages(merged);
      };

      load(selectedConversationId);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedConversationId]);

    // Effect 2: Rebuild system message and merge with current messages when MCP/systemInstructions/resources change
    useEffect(() => {
      // Only run if there are messages and a selected conversation
      if (!selectedConversationId) return;
      setMessages((prev: any[]) => {
        if (!prev || prev.length === 0) return [systemMsg];
        // Remove all system messages, prepend the new one
        const nonSystem = prev.filter((m: any) => m.role !== "system");
        return [systemMsg, ...nonSystem];
      });
    }, [systemMsg, selectedConversationId]);

    // Determine if there are any non-system messages
    const hasNonSystemMessages = currentMessages.some(
      (m: any) => m.role !== "system"
    );

    const lastPart = useMemo(() => {
      const lastMsg =
        currentMessages.length > 0
          ? currentMessages[currentMessages.length - 1]
          : undefined;
      if (
        lastMsg &&
        lastMsg.role === "assistant" &&
        Array.isArray(lastMsg.parts) &&
        lastMsg.parts.length > 0
      ) {
        const lastPart = lastMsg.parts[lastMsg.parts.length - 1];
        if (
          lastPart.type?.startsWith("tool-") &&
          typeof lastPart.state === "string" &&
          lastPart.state.startsWith("input-")
        ) {
          return lastPart;
        }
      }
      return undefined;
    }, [currentMessages]);

    // Empty-state: no non-system messages and input is visible
    if (!hasNonSystemMessages && !hideInput) {
      return (
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            height: "100%",
            alignItems: "center",
            justifyContent: "center",
            border: isOver ? "2px dotted" : undefined,
            borderColor: isOver ? "#888" : "transparent",
            textAlign: "center",
          }}
          ref={dropRef}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
        >
          <div style={{ width: "95%" }}>
            <WelcomeMessage
              status={status}
              selectedConversationId={selectedConversationId}
            />

            <MessageInput
              model={model}
              onSend={send}
              disabled={pending}
              onStop={() => {
                stop();
                stopTool?.();
              }}
              streaming={pending}
            />
          </div>
        </div>
      );
    }

    // Default: show chat as usual
    return (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          height: "100%",
          border: isOver ? "2px dotted" : undefined,
          borderColor: isOver ? "#888" : "transparent",
        }}
        ref={dropRef}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
      >
        <ChatErrors />

        <MessageList
          showToolsDrawer={showToolsDrawer}
          showCitations={showCitations}
        />
        {status === "submitted" || lastPart ? (
          <Spinner
            label={
              lastPart ? `${lastPart?.type.replace("tool-", "")}...` : undefined
            }
          />
        ) : undefined}
        {!hideInput && (
          <div style={{ paddingRight: 24 }}>
            <MessageInput
              model={model}
              onSend={send}
              disabled={pending}
              onStop={() => {
                stop();
                stopTool?.();
              }}
              streaming={pending}
            />
          </div>
        )}
      </div>
    );
  }
);
//style={{ paddingLeft: 16, paddingRight: 16 }}
