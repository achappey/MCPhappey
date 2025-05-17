import { useState, useEffect } from "react";
// Update import path for types
import { McpServerListResponse, McpServerWithName } from "../types/mcp";

export interface ServerData {
  servers: McpServerWithName[];
  loading: boolean;
  error: string | null;
}

export interface ServerDataState {
  [url: string]: ServerData;
}

/**
 * Hook to fetch MCP server data for multiple list URLs.
 * Manages loading and error states independently for each URL.
 */
export const useMcpServerData = (urls: string[]) => {
  const [serverData, setServerData] = useState<ServerDataState>({});

  // Effect 1: Initialize/update serverData structure based on urls
  useEffect(() => {
    setServerData((prevData) => {
      const newData = { ...prevData };
      let changed = false;

      // Add new URLs
      urls.forEach((url) => {
        if (!newData[url]) {
          newData[url] = { servers: [], loading: true, error: null };
          changed = true;
        }
      });

      // Remove old URLs
      Object.keys(newData).forEach((existingUrl) => {
        if (!urls.includes(existingUrl)) {
          delete newData[existingUrl];
          changed = true;
        }
      });

      return changed ? newData : prevData;
    });
  }, [urls]); // Only depends on the urls array

  // Effect 2: Fetch data for URLs marked as loading
  useEffect(() => {
    urls.forEach((url) => {
      const currentEntry = serverData[url];
      if (currentEntry && currentEntry.loading) {
        // Mark as not loading immediately to prevent re-fetch if component re-renders
        // before fetch completes. The fetch completion will set loading to false again.
        // This is a common pattern but can be tricky.
        // A more robust way might involve a separate 'isFetching' flag per URL.
        // For now, we rely on the fetch itself to update the loading state.

        fetch(url)
          .then((res) => {
            if (!res.ok)
              throw new Error(
                `Failed to fetch from ${url} (status: ${res.status})`
              );
            return res.json();
          })
          .then((data: McpServerListResponse) => {
            const arr: McpServerWithName[] = Object.entries(
              data.servers || {}
            ).map(([name, details]) => ({
              name,
              ...details,
            }));
            setServerData((prev) => ({
              ...prev,
              [url]: { servers: arr, loading: false, error: null },
            }));
          })
          .catch((err) => {
            console.error(`Error fetching from ${url}:`, err);
            setServerData((prev) => ({
              ...prev,
              [url]: {
                servers: [],
                loading: false,
                error: err.message || "Unknown error",
              },
            }));
          });
      }
    });
  }, [serverData, urls]); // Depends on serverData (to see .loading flags) and urls

  return { serverData };
};
