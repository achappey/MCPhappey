/**
 * Minimal, framework-agnostic JSON fetcher with error handling and timeout.
 * No React, no storage, no side effects.
 */
export interface HttpError {
    url: string;
    status?: number;
    message: string;
}
export type Result<T> = {
    ok: true;
    data: T;
} | {
    ok: false;
    error: HttpError;
};
/**
 * Fetches JSON from a URL with a 10s timeout.
 * Returns a Result<T> with error details on failure.
 */
export declare const getJson: <T = unknown>(url: string, init?: RequestInit) => Promise<Result<T>>;
//# sourceMappingURL=getJson.d.ts.map