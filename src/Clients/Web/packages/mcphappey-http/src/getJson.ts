/**
 * Minimal, framework-agnostic JSON fetcher with error handling and timeout.
 * No React, no storage, no side effects.
 */

export interface HttpError {
  url: string;
  status?: number;
  message: string;
}

export type Result<T> =
  | { ok: true; data: T }
  | { ok: false; error: HttpError };

/**
 * Fetches JSON from a URL with a 10s timeout.
 * Returns a Result<T> with error details on failure.
 */
export const getJson = async <T = unknown>(
  url: string,
  init?: RequestInit
): Promise<Result<T>> => {
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

    const data = (await res.json()) as T;
    return { ok: true, data };
  } catch (err: any) {
    clearTimeout(timeout);
    let message = "Unknown error";
    if (err?.name === "AbortError") message = "Request timed out";
    else if (typeof err?.message === "string") message = err.message;

    return {
      ok: false,
      error: {
        url,
        message,
      },
    };
  }
};
