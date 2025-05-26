import { Result } from "./getJson";
import type { McpServerWithName } from "mcphappey-types";
/**
 * Fetches and validates an MCP server list JSON file.
 * Returns a Result with an array of McpServerWithName or an error.
 */
export declare const fetchServerList: (url: string) => Promise<Result<McpServerWithName[]>>;
//# sourceMappingURL=fetchServerList.d.ts.map