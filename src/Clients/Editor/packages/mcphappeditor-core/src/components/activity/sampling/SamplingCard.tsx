import React from "react";
import { useTheme } from "../../../ThemeContext";
import { ChatMessage } from "mcphappeditor-types";
import { Markdown } from "../../markdown/Markdown";
import { SamplingRequest } from "mcphappeditor-state";

export interface SamplingCardProps {
  notif: SamplingRequest;
}

export const SamplingCard: React.FC<SamplingCardProps> = ({ notif }) => {
  const { Chat, Spinner } = useTheme();
  //  const flatMapped = notif[0].messages.flatMap((e) => e.content);
  const flatMapped: ChatMessage[] = notif[1].params.messages.flatMap((msg) => ({
    id: crypto.randomUUID(),
    role: msg.role,
    text: msg.content.text as string,
    content: <Markdown text={msg.content.text as string} />,
  }));

  if (notif[2]) {
    flatMapped.push({
      id: crypto.randomUUID(),
      role: notif[2].role,
      content: <Markdown text={notif[2].content.text as string} />,
      text: notif[2].content.text as string,
      author: notif[2].model,
    });
  }

  return (
    <>
      <Chat messages={flatMapped} />
      {!notif[2] ? <Spinner /> : undefined}
    </>
  );
};
