/**
 * Saves the access token for a given server URL to localStorage.
 * @param serverUrl The URL of the MCP server.
 * @param token The access token.
 */
export declare const saveAccessToken: (serverUrl: string, token: string) => void;
/**
 * Retrieves the access token for a given server URL from localStorage.
 * @param serverUrl The URL of the MCP server.
 * @returns The access token string, or null if not found or if localStorage is unavailable.
 */
export declare const getAccessToken: (serverUrl: string) => string | null;
/**
 * Clears the access token for a given server URL from localStorage.
 * @param serverUrl The URL of the MCP server.
 */
export declare const clearAccessToken: (serverUrl: string) => void;
//# sourceMappingURL=storage.d.ts.map