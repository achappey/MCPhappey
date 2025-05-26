/**
 * Minimal, framework-agnostic JSON fetcher with error handling and timeout.
 * No React, no storage, no side effects.
 */
/**
 * Fetches JSON from a URL with a 10s timeout.
 * Returns a Result<T> with error details on failure.
 */
export const getJson = async (url, init) => {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10000);
    try {
        const res = await fetch(url, { ...init, signal: controller.signal });
        clearTimeout(timeout);
        if (!res.ok) {
            return {
                ok: false,
                error: {
                    url,
                    status: res.status,
                    message: `HTTP ${res.status} ${res.statusText}`,
                },
            };
        }
        const data = (await res.json());
        return { ok: true, data };
    }
    catch (err) {
        clearTimeout(timeout);
        let message = "Unknown error";
        if (err?.name === "AbortError")
            message = "Request timed out";
        else if (typeof err?.message === "string")
            message = err.message;
        return {
            ok: false,
            error: {
                url,
                message,
            },
        };
    }
};
//# sourceMappingURL=getJson.js.map