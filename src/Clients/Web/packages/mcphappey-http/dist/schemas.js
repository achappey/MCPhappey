import { z } from "zod";
/**
 * Zod schema for a single MCP server entry.
 * Allows extra properties for extensibility.
 */
export const mcpServerSchema = z.object({
    type: z.string(),
    url: z.string().url(),
    headers: z.record(z.string(), z.string()).optional(),
}).passthrough();
/**
 * Zod schema for the MCP server list JSON file.
 * The servers object maps name → server definition.
 */
export const mcpServerListResponseSchema = z.object({
    servers: z.record(z.string(), mcpServerSchema),
});
//# sourceMappingURL=schemas.js.map