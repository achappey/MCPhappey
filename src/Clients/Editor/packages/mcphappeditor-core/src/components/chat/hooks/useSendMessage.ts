import { useAppStore } from "mcphappeditor-state";
import { fileToDataUrl, extractTextFromZip, extractTextFromFile } from "../utils/file";
import { toMarkdownLinkSmart } from "../utils/markdown";
import { useEnqueueUserMessage } from "./useEnqueueUserMessage";

interface UseSendMessageProps {
  model?: string;
  temperature?: number
  conversationId?: string;
}

export const useSendMessage = ({ model, temperature }: UseSendMessageProps) => {
  const resourceResults = useAppStore((s) => s.resourceResults);
  const clearResourceResults = useAppStore((s) => s.clearResourceResults);
  const attachments = useAppStore((s) => s.attachments);
  const clearAttachments = useAppStore((s) => s.clearAttachments);
  const enqueue = useEnqueueUserMessage();

  const send = async (content: string) => {
    const resourceParts: any[] = [
      ...resourceResults.flatMap((r: any) =>
        r.data.contents.map((z: any) => z.text ? ({
          type: "text",
          text: toMarkdownLinkSmart(z.uri, z.text as string, z.mimeType),
        }) : ({
          type: "file",
          mediaType: z.mimeType,
          url: `data:${z.mimeType};base64,${z.blob}`
        }))
      ),
    ];

    const textAttachments: any[] = [];

    for (const a of attachments) {
      if (a.type === "application/zip" || /\.zip$/i.test(a.name)) {
        textAttachments.push(...await extractTextFromZip(a));
      } else {
        const text = await extractTextFromFile(a);
        if (text) {
          textAttachments.push({
            type: "text",
            text: toMarkdownLinkSmart(a.name, text, a.type)
          });
        }
      }
    }

    const attachmentParts = attachments
      ? await Promise.all(
        attachments.map(async (a: any) => ({
          type: "file",
          filename: a.name,
          mediaType: a.type,
          url: await fileToDataUrl(a.file as File),
        }))
      )
      : [];

    const textParts = content.trim()
      ? [{ type: "text", text: content.trim() }]
      : [];

    const parts: any[] = [
      ...resourceParts,
      ...textAttachments,
      ...attachmentParts,
      ...textParts,
    ];

    await enqueue(parts, model, temperature);
    clearResourceResults();
    clearAttachments();
  };

  return { send };
};
