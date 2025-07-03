// Thin REST helper for MCPHappeditor API
import { createHttpClient, HttpClientConfig } from 'mcphappeditor-http';

export const createRest = (cfg: HttpClientConfig) => {
  const base = createHttpClient(cfg);

  const withBody = async <T>(method: string, url: string, body?: unknown) => {
    const res = await (cfg.fetch || fetch)(url, {
      method,
      headers: { 'Content-Type': 'application/json', ...(cfg.headers || {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}: ${res.statusText}`);
    // void endpoints (204) return no content
    if (res.status === 204) return undefined as T;
    return res.json() as Promise<T>;
  };

  return {
    get: base.get,
    post: <TRes>(url: string, body: unknown) => withBody<TRes>('POST', url, body),
    put: <TRes>(url: string, body: unknown) => withBody<TRes>('PUT', url, body),
    del: <TRes>(url: string) => withBody<TRes>('DELETE', url),
  };
};