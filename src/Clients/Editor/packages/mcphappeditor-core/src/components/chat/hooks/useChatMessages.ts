import { useAiChatMessages } from "mcphappeditor-ai";
import { MessageRole } from "mcphappeditor-types";
import { useMemo } from "react";
import { format } from "timeago.js";

export interface ChatMessageData {
  contentText: string;
  role: MessageRole;
  id: string;
  author?: string;
  totalTokens?: number;
  temperature?: number;
  createdAt: string;
  sources: { title: string; url: string }[];
  tools: any[];
  isImage: boolean;
}

export const useChatMessages = () => {
  const messages = useAiChatMessages();

  return useMemo<ChatMessageData[]>(() => {
    return messages
      .filter((a) => a.role !== "system")
      .flatMap((r: any) => {
        const sources =
          r.parts
            ?.filter((p: any) => p.type === "source-url")
            .map((p: any) => ({ title: p.title, url: p.url })) ?? [];

        return r.parts.flatMap((z: any, idx: number) => {
          const tools =
            r.parts?.filter((p: any) => p.type.startsWith("tool-")) ?? [];

          // 1. Text, reasoning, or image part
          if (
            z.type === "text" ||
            z.type === "reasoning" ||
            (z.type === "file" && z.mediaType?.startsWith("image/"))
          ) {
            let contentText: string;
            let isImage = false;
            if (z.type === "file" && z.mediaType?.startsWith("image/")) {
              const alt = z.filename || "image";
              contentText = `![${alt}](${z.url})`;
              isImage = true;
            } else {
              contentText = z.text;
            }
            return [
              {
                contentText,
                role: r.role,
                id: r.id + "-" + idx,
                author: r.metadata?.author ?? r.metadata?.model,
                totalTokens: r.metadata?.totalTokens,
                temperature: r.metadata?.temperature,
                createdAt: format(r.metadata?.timestamp),
                sources,
                tools,
                isImage,
                // download and copyToClipboard will be assigned in the component
              },
            ];
          }

          // 2. Tool parts with embedded images
          if (
            z.type?.startsWith("tool-") &&
            z.output &&
            Array.isArray(z.output.content)
          ) {
            return z.output.content
              .filter(
                (item: any) =>
                  item.type === "image" && typeof item.data === "string"
              )
              .map((item: any, imgIdx: number) => {
                const alt = z.toolCallId || "tool-image";
                const url = `data:${item.mimeType};base64,${item.data}`;
                const contentText = `![${alt}](${url})`;
                return {
                  contentText,
                  role: r.role,
                  id: r.id + "-toolimg-" + idx + "-" + imgIdx,
                  author: r.metadata?.author ?? r.metadata?.model,
                  createdAt: format(r.metadata?.timestamp),
                  sources,
                  tools,
                  isImage: true,
                };
              });
          }

          return [];
        });
      });
  }, [messages]);
};
