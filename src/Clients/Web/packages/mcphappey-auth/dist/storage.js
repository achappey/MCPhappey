const ACCESS_TOKEN_STORAGE_PREFIX = "mcp_access_token_";
/**
 * Saves the access token for a given server URL to localStorage.
 * @param serverUrl The URL of the MCP server.
 * @param token The access token.
 */
export const saveAccessToken = (serverUrl, token) => {
    try {
        localStorage.setItem(`${ACCESS_TOKEN_STORAGE_PREFIX}${serverUrl}`, token);
    }
    catch (e) {
    }
};
/**
 * Retrieves the access token for a given server URL from localStorage.
 * @param serverUrl The URL of the MCP server.
 * @returns The access token string, or null if not found or if localStorage is unavailable.
 */
export const getAccessToken = (serverUrl) => {
    try {
        return localStorage.getItem(`${ACCESS_TOKEN_STORAGE_PREFIX}${serverUrl}`);
    }
    catch (e) {
        return null;
    }
};
/**
 * Clears the access token for a given server URL from localStorage.
 * @param serverUrl The URL of the MCP server.
 */
export const clearAccessToken = (serverUrl) => {
    try {
        localStorage.removeItem(`${ACCESS_TOKEN_STORAGE_PREFIX}${serverUrl}`);
    }
    catch (e) {
    }
};
//# sourceMappingURL=storage.js.map