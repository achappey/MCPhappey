/**
 * Fetch with Authorization header (if token exists) and 401/403 detection.
 * @param url The resource URL.
 * @param init Optional fetch options.
 * @returns {Promise<{ unauthorised: boolean; res?: Response }>}
 */
export declare const authFetch: (url: string, init?: RequestInit) => Promise<{
    unauthorised: boolean;
    res?: Response;
}>;
//# sourceMappingURL=authFetch.d.ts.map