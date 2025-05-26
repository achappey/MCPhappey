import { getJson, Result, HttpError } from "./getJson";
import { mcpServerListResponseSchema } from "./schemas";
import type { McpServerWithName } from "mcphappey-types";

/**
 * Fetches and validates an MCP server list JSON file.
 * Returns a Result with an array of McpServerWithName or an error.
 */
export const fetchServerList = async (
  url: string
): Promise<Result<McpServerWithName[]>> => {
  const res = await getJson<unknown>(url);
  if (!res.ok) return res;

  // Validate structure
  const parsed = mcpServerListResponseSchema.safeParse(res.data);
  if (!parsed.success) {
    return {
      ok: false,
      error: {
        url,
        message: "Invalid MCP server list format",
      } as HttpError,
    };
  }

  // Flatten servers object to array with name injected
  const serversObj = parsed.data.servers;
  const servers: McpServerWithName[] = Object.entries(serversObj).map(
    ([name, details]) => ({
      name,
      ...details,
    })
  );

  return { ok: true, data: servers };
};
