import { useAiChatAppend } from "mcphappeditor-ai";
import { useAppStore } from "mcphappeditor-state";
import { useConversations } from "mcphappeditor-conversations";
import { useAccount } from "mcphappeditor-auth";
import type { UIMessage } from "mcphappeditor-types";

export const useEnqueueUserMessage = () => {
    const append = useAiChatAppend();
    const account = useAccount();
    const tools = useAppStore((s) => s.tools);
    const selectedConversationId = useAppStore((s) => s.selectedConversationId);
    const selectConversation = useAppStore((s) => s.selectConversation);
    const defaultTemperature = useAppStore((s) => s.temperature);
    const { addMessage, create } = useConversations();

    return async (parts: UIMessage["parts"], model?: string, temperature?: number) => {
        let convId = selectedConversationId;
        let effTemp = temperature ?? defaultTemperature;
        if (!convId) {
            const conv = await create("New chat", effTemp);
            convId = conv.id;
            selectConversation(convId);
        }

        const userMessage: UIMessage = {
            id: crypto.randomUUID(),
            role: "user",
            parts,
            metadata: {
                timestamp: new Date().toISOString(),
                author: account?.name,
            },
        };

        if (typeof window !== "undefined") {
            const w = window as any;

            // Ensure a global ref for id → conversationId mapping exists
            if (!w.ChatProvider_pendingRef) {
                w.ChatProvider_pendingRef = { current: {} };
            }
            if (!w.ChatProvider_pendingRef.current) {
                w.ChatProvider_pendingRef.current = {};
            }
            w.ChatProvider_pendingRef.current[userMessage.id] = convId;

            // Cache the full user message so ChatPanel can rehydrate it
            if (!w.ChatProvider_pendingMessages) {
                w.ChatProvider_pendingMessages = {};
            }
            w.ChatProvider_pendingMessages[userMessage.id] = userMessage;
        }

        await append(
            userMessage,
            {
                body: {
                    model,
                    temperature: effTemp,
                    tools: Object.values(tools).flat(),
                },
            }
        );

        if (convId) {
            await addMessage(convId, userMessage);
        }
    };
};
