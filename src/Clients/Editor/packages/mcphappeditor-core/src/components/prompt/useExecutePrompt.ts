import { useAppStore } from "mcphappeditor-state";
import { toMarkdownLinkSmart } from "../chat/utils/markdown";
import type { Prompt } from "mcphappeditor-mcp";
import { useEnqueueUserMessage } from "../chat/hooks/useEnqueueUserMessage";

type PromptWithSource = Prompt & { _url: string };

export const useExecutePrompt = (model?: string) => {
    const clients = useAppStore((a) => a.clients);
    const enqueue = useEnqueueUserMessage();
    // Returns a promise, so caller can await or fire-and-forget
    return async (prompt: PromptWithSource, args?: Record<string, string>) => {
        const client = clients?.[prompt._url];
        if (!client || typeof (client as any).getPrompt !== "function") {
            throw new Error("Server client not available.");
        }
        const argumentsObj = args ?? {};
        const result = await (client as any).getPrompt({
            name: prompt.name,
            arguments: argumentsObj,
        });
        const messages = result.messages ?? [];
        const parts = messages.map((m: any) => ({
            type: "text",
            text:
                m.content.text ??
                toMarkdownLinkSmart(
                    m.content.resource.uri,
                    m.content.resource.text as string,
                    m.content.resource.mimeType
                ),
        }));
        await enqueue(parts, model);
    };
};
