export { saveAccessToken, getAccessToken, clearAccessToken } from "./storage";
/**
 * Initiates the OAuth 2.1 Authorization Code Flow with PKCE and Dynamic Client Registration.
 * @param mcpServerUrl The URL of the MCP server requiring authentication.
 */
export declare const initiateOAuthFlow: (mcpServerUrl: string) => Promise<void>;
/**
 * Handles the OAuth callback by exchanging the authorization code for an access token.
 * @returns A Promise resolving to an object with the access token and target URL, or an error.
 */
export declare const handleOAuthCallback: () => Promise<{
    accessToken: string;
    targetUrl?: string;
} | {
    error: string;
    errorDescription?: string;
}>;
//# sourceMappingURL=oauthFlow.d.ts.map