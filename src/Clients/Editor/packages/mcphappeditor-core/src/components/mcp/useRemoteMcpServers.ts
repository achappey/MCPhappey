import { useAppStore } from "mcphappeditor-state";
import { useEffect } from "react";

export const useRemoteMcpServers = (configUrls: string[] = []) => {
  const endpoints = useAppStore((s) => s.endpoints);
  const setEndpoints = useAppStore((s) => s.setEndpoints);
  const refreshRemoteServers = useAppStore((s) => s.refreshRemoteServers);

  // On first load, seed endpoints with configUrls if empty
  useEffect(() => {
    if (endpoints.length === 0 && configUrls.length) {
      setEndpoints(configUrls);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [JSON.stringify(configUrls), endpoints.length]);

  // Whenever endpoints changes, refresh remote servers
  useEffect(() => {
    if (endpoints.length > 0) {
      refreshRemoteServers();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [JSON.stringify(endpoints)]);
};
