export interface HttpClientConfig {
  getAccessToken?: () => Promise<string>;
  headers?: Record<string, string>;
  fetch?: typeof fetch;
}

export const createHttpClient = (cfg: HttpClientConfig = {}) => {
  const doFetch = async (input: RequestInfo, init: RequestInit = {}) => {
    let headers: Record<string, string> = { ...(cfg.headers || {}) };

    if (cfg.getAccessToken) {
      const token = await cfg.getAccessToken();
      headers['Authorization'] = `Bearer ${token}`;
    }
    if (init.headers) headers = { ...headers, ...(init.headers as any) };

    return (cfg.fetch || fetch)(input, { ...init, headers });
  };

  return {
    get: <T>(url: string) => doFetch(url).then(r => r.json() as Promise<T>),
    // put/post/delete helpers could be added here later
  };
};
