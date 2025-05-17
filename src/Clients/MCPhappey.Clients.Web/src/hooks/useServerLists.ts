import { useState, useEffect, useCallback } from "react";
import { DEFAULT_MCP_SERVER_LIST_URLS } from "../config";

const LOCAL_STORAGE_KEY = "customMcpServerListUrls";

/**
 * Hook to manage the list of MCP server list URLs.
 * Combines default URLs from config with user-added URLs from localStorage.
 */
export const useServerLists = () => {
  const [customUrls, setCustomUrls] = useState<string[]>(() => {
    try {
      const storedUrls = localStorage.getItem(LOCAL_STORAGE_KEY);
      return storedUrls ? JSON.parse(storedUrls) : [];
    } catch (error) {
      console.error("Error reading custom URLs from localStorage:", error);
      return [];
    }
  });

  // Update localStorage whenever customUrls change
  useEffect(() => {
    try {
      localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(customUrls));
    } catch (error) {
      console.error("Error saving custom URLs to localStorage:", error);
    }
  }, [customUrls]);

  const addUrl = useCallback((url: string) => {
    if (url && !customUrls.includes(url) && !DEFAULT_MCP_SERVER_LIST_URLS.includes(url)) {
      setCustomUrls((prev) => [...prev, url]);
    }
  }, [customUrls]);

  const removeUrl = useCallback((url: string) => {
    setCustomUrls((prev) => prev.filter((u) => u !== url));
  }, []);

  // Combine default and custom URLs, ensuring uniqueness
  const allUrls = Array.from(new Set([...DEFAULT_MCP_SERVER_LIST_URLS, ...customUrls]));

  return { allUrls, customUrls, addUrl, removeUrl };
};
