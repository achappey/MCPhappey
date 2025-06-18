import { getJson } from "./getJson";
import { mcpServerListResponseSchema } from "./schemas";
/**
 * Fetches and validates an MCP server list JSON file.
 * Returns a Result with an array of McpServerWithName or an error.
 */
export const fetchServerList = async (url) => {
    const res = await getJson(url);
    if (!res.ok)
        return res;
    // Validate structure
    const parsed = mcpServerListResponseSchema.safeParse(res.data);
    if (!parsed.success) {
        return {
            ok: false,
            error: {
                url,
                message: "Invalid MCP server list format",
            },
        };
    }
    // Flatten servers object to array with name injected
    const serversObj = parsed.data.servers;
    const servers = Object.entries(serversObj).map(([name, details]) => ({
        name,
        ...details,
    }));
    return { ok: true, data: servers };
};
//# sourceMappingURL=fetchServerList.js.map