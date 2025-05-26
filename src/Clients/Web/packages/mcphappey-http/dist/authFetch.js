import { getAccessToken } from "mcphappey-auth/src";
/**
 * Fetch with Authorization header (if token exists) and 401/403 detection.
 * @param url The resource URL.
 * @param init Optional fetch options.
 * @returns {Promise<{ unauthorised: boolean; res?: Response }>}
 */
export const authFetch = async (url, init) => {
    const token = getAccessToken(url);
    const headers = {
        ...(init?.headers || {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
    };
    const res = await fetch(url, { ...init, headers });
    if (res.status === 401 || res.status === 403) {
        return { unauthorised: true };
    }
    return { unauthorised: false, res };
};
//# sourceMappingURL=authFetch.js.map