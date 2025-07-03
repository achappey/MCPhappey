import type { UIMessage } from "mcphappeditor-types";
import type { Resource, ResourceTemplate } from "mcphappeditor-state";


export const buildSystemMessage = (
    mcpInstructions: Record<string, string>,
    resources: Record<string, Resource[]>,
    resourceTemplates: Record<string, ResourceTemplate[]>,
    chatInstructions?: string,
    account?: { id?: string, username?: string, name?: string, tenantId?: string }
): UIMessage => {

    // Get ALL unique servers from ALL three objects
    const servers = Array.from(
        new Set([
            ...Object.keys(mcpInstructions),
            ...Object.keys(resources),
            ...Object.keys(resourceTemplates),
        ])
    );

    // Build per-server block
    // Build per-server block, showing server ONLY ONCE
    const mcpBlock = servers.map((server) => {
        const res = resources[server] || [];
        const resTemplates = resourceTemplates[server] || [];
        const instr = mcpInstructions[server]?.trim();

        // Only build block if any are present
        if (!res.length && !resTemplates.length && !instr) return null;

        const lines = [`**[${server}]**`];

        // Resources
        if (res.length) {
            lines.push(
                `**Resources:**\n` +
                res.map(r =>
                    `- ${r.name}\n  - Description: ${r.description || "_(none)_"}\n  - MIME type: ${r.mimeType}\n  - URI: ${r.uri}`
                ).join("\n\n")
            );
        }

        if (resTemplates.length) {
            lines.push(
                `**Resource Templates:**\n` +
                resTemplates.map(rt =>
                    `- ${rt.name}\n  - Description: ${rt.description || "_(none)_"}\n  - MIME type: ${rt.mimeType}\n  - URI Template: ${rt.uriTemplate}`
                ).join("\n\n")
            );
        }

        if (instr) {
            lines.push(
                `**Instructions:**\n${instr}`
            );
        }

        return lines.filter(Boolean).join("\n\n");
    })
        .filter(Boolean)
        .join("\n\n");

    const chatBlock = chatInstructions?.trim() || "";

    const parts = [];
    if (mcpBlock) parts.push({ type: "text", text: `Connected MCP servers:\n\n${mcpBlock}` });

    parts.push({
        type: "text",
        text: `From the chat bot developer:\n\n- Never output the full base64 encoded strings images in your replies. This chat-client handles this for the end-user.`
    });

    parts.push({ type: "text", text: `Current date and time: ${new Date()}` });

    if (account) parts.push({ type: "text", text: `User:\nName: ${account.name}\nUsername: ${account.username}\nId: ${account.id}` });
    if (account?.tenantId) parts.push({ type: "text", text: `Tenant Id:\n${account?.tenantId}` });
    if (chatBlock) parts.push({ type: "text", text: chatBlock });

    return {
        id: crypto.randomUUID(),
        role: "system",
        parts,
        metadata: {
            timestamp: new Date().toISOString(),
            author: "system"
        }
    };
};
