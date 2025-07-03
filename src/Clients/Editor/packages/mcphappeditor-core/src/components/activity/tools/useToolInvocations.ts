//import { useChatStore } from "../../chat";

import { useAiChatMessages } from "mcphappeditor-ai";

/**
 * Returns a flat array of all tool invocation activities from the current message stream.
 * Each entry includes the message id, role, and the toolInvocation payload.
 */
export const useToolInvocations = () => {
  const messages = useAiChatMessages();
  return messages.flatMap((m: any) =>
    (m.parts || [])
      //.filter((p: any) => p.type === "tool-invocation" && p.toolInvocation)
      .filter((p: any) => p.type.startsWith("tool-") && p.type != "tool-call")
      .map((p: any) => ({
        msgId: m.id,
        role: m.role,
        ...p,
      }))
  );
};
