import { useCallback } from "react";
import { useServerPrimitivesStore } from "mcphappey-state/src/slices/servers";
import { useMcpConnect } from "./useMcpConnect";

/**
 * Hook to access and load server primitives for a given serverUrl.
 */
export function useServerPrimitives(serverUrl: string | null) {
  const { connect } = useMcpConnect();
  const state = useServerPrimitivesStore((s) =>
    serverUrl ? s.servers[serverUrl] || { loading: false } : { loading: false }
  );
  const {
    loadCapabilities,
    loadPrompts,
    loadResources,
    loadTools,
  } = useServerPrimitivesStore();

  // Helper to ensure client is connected before loading
  const loadAll = useCallback(async () => {
    if (!serverUrl) return;
    const client = await connect(serverUrl);
    await loadCapabilities(serverUrl, client);
    // After capabilities, load primitives if supported
    const cap = useServerPrimitivesStore.getState().servers[serverUrl]?.capabilities;
    if (cap?.prompts) await loadPrompts(serverUrl, client);
    if (cap?.resources) await loadResources(serverUrl, client);
    if (cap?.tools) await loadTools(serverUrl, client);
  }, [serverUrl, connect, loadCapabilities, loadPrompts, loadResources, loadTools]);

  return {
    ...state,
    loadCapabilities: useCallback(async () => {
      if (!serverUrl) return;
      const client = await connect(serverUrl);
      await loadCapabilities(serverUrl, client);
    }, [serverUrl, connect, loadCapabilities]),
    loadPrompts: useCallback(async () => {
      if (!serverUrl) return;
      const client = await connect(serverUrl);
      await loadPrompts(serverUrl, client);
    }, [serverUrl, connect, loadPrompts]),
    loadResources: useCallback(async () => {
      if (!serverUrl) return;
      const client = await connect(serverUrl);
      await loadResources(serverUrl, client);
    }, [serverUrl, connect, loadResources]),
    loadTools: useCallback(async () => {
      if (!serverUrl) return;
      const client = await connect(serverUrl);
      await loadTools(serverUrl, client);
    }, [serverUrl, connect, loadTools]),
    loadAll,
  };
}
