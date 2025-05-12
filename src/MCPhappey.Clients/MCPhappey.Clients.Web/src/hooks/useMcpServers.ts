import { useEffect, useState } from "react";

export interface McpServer {
  type: string;
  url: string;
  headers?: Record<string, string>;
  [key: string]: any;
}

export interface McpServerListResponse {
  servers: {
    [name: string]: McpServer;
  };
}

export interface McpServerWithName extends McpServer {
  name: string;
}

export function useMcpServers(fetchUrl: string) {
  const [servers, setServers] = useState<McpServerWithName[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    fetch(fetchUrl)
      .then((res) => {
        if (!res.ok) throw new Error("Failed to fetch MCP servers");
        return res.json();
      })
      .then((data: McpServerListResponse) => {
        const arr: McpServerWithName[] = Object.entries(data.servers).map(
          ([name, details]) => ({
            name,
            ...details,
          })
        );
        setServers(arr);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.message || "Unknown error");
        setLoading(false);
      });
  }, [fetchUrl]);

  return { servers, loading, error };
}
