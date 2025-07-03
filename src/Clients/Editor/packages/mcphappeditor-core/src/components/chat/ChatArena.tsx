import React, { useRef, useState } from "react";
import { useConversations } from "mcphappeditor-conversations";
import { ChatPanel } from "./ChatPanel";
import { MessageInput } from "./MessageInput";
//import { useChatStore } from "./useChatStore";
/*
export const ChatArena: React.FC<{
  leftModel: string;
  rightModel: string;
}> = ({ leftModel, rightModel }) => {
  const { createConversation } = useConversations();
  // Create two conversations for the arena (one per model)
  const [convLeft] = useState(() => createConversation(leftModel));
  const [convRight] = useState(() => createConversation(rightModel));

  // Refs to ChatPanel so we can call send() on both
  const leftRef = useRef<ChatPanelHandle>(null);
  const rightRef = useRef<ChatPanelHandle>(null);

  // Get pending status for both conversations
  const { status: leftStatus } = useChatStore({ id: convLeft.id });
  const { status: rightStatus } = useChatStore({ id: convRight.id });
  const pending =
    leftStatus === "submitted" ||
    leftStatus === "streaming" ||
    rightStatus === "submitted" ||
    rightStatus === "streaming";

  const handleSend = (text: string) => {
    const trimmed = text.trim();
    if (!trimmed) return;
    leftRef.current?.send(trimmed);
    rightRef.current?.send(trimmed);
  };

  return (
    <div
      style={{
        display: "flex",
        height: "100%",
        overflowX: "hidden",
        overflow: "hidden", // ← add
        minWidth: 0,
      }}
    >
      <div
        style={{
          flex: 1,
          borderRight: "1px solid #ddd",
          boxSizing: "border-box",
          minWidth: 0,
        }}
      >
        <ChatPanel
          ref={leftRef}
          conversationId={convLeft.id}
          model={leftModel}
          hideInput
        />
      </div>
      <div style={{ flex: 1, minWidth: 0, boxSizing: "border-box" }}>
        <ChatPanel
          ref={rightRef}
          conversationId={convRight.id}
          model={rightModel}
          hideInput
        />
      </div>
      <div
        style={{
          position: "fixed",
          left: 260,
          right: 0,
          bottom: 0,
          zIndex: 10,
          padding: 12,
        }}
      >
        <MessageInput
          model={leftModel}
          onSend={handleSend}
          disabled={pending}
        />
      </div>
    </div>
  );
};
*/