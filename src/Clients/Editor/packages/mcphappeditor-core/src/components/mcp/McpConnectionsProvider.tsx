import { ReactNode, useEffect, useRef } from "react";
import { acquireAccessToken } from "mcphappeditor-auth";
import {
  CreateMessageRequest,
  CreateMessageResult,
  ElicitRequest,
  ElicitResult,
  LoggingMessageNotification,
  ProgressNotification,
} from "mcphappeditor-mcp";
import { useAppStore } from "mcphappeditor-state";

// Pending Elicit promise resolvers, not in state
const pendingElicits: Record<string, (r: ElicitResult) => void> = {};

/**
 * Call this to resolve an ElicitRequest by id.
 */
export const respondElicit = (id: string, result: ElicitResult) => {
  if (pendingElicits[id]) {
    pendingElicits[id](result);
    delete pendingElicits[id];
  }
};

type McpConnectionsProviderProps = {
  children: ReactNode;
  samplingApi?: string;
  clientName?: string;
  clientVersion?: string;
};

/**
 * McpConnectionsProvider - Ensures all selected MCP servers are connected.
 * Place this high in the component tree (e.g. in CoreRoot).
 * Uses zustand store for state and actions.
 */
export const McpConnectionsProvider = ({
  children,
  samplingApi,
  clientVersion,
  clientName,
}: McpConnectionsProviderProps) => {
  const selected = useAppStore((a) => a.selected);
  const servers = useAppStore((a) => a.servers);
  const addProgress = useAppStore((a) => a.addProgress);
  const addNotification = useAppStore((a) => a.addNotification);
  const addSamplingRequest = useAppStore((a) => a.addSamplingRequest);
  const addSamplingResponse = useAppStore((a) => a.addSamplingResponse);
  const addElicitRequest = useAppStore((a) => a.addElicitRequest);
  const addElicitResponse = useAppStore((a) => a.addElicitResponse);
  const connect = useAppStore((a) => a.connect);

  useEffect(() => {
    const urls = selected.map((n) => servers[n]?.url).filter(Boolean);
    const activeServers = selected.map((n) => servers[n]).filter(Boolean);

    const onElicit = (server: string, params: ElicitRequest) => {
      const id = crypto.randomUUID();
      addElicitRequest(id, server, params);
      return new Promise<ElicitResult>((resolve) => {
        pendingElicits[id] = (result: ElicitResult) => {
          addElicitResponse(id, server, result);
          resolve(result);
        };
      });
    };

    // Only create the callback once per render
    const onSample = samplingApi
      ? async (server: string, params: CreateMessageRequest) => {
          const id = crypto.randomUUID();
          addSamplingRequest(id, server, params);
          const accessToken = await acquireAccessToken();
          const res = await fetch(samplingApi, {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Authorization: `Bearer ${accessToken}`,
            },
            body: JSON.stringify(params.params),
          });

          if (!res.ok) {
            throw new Error(`Sampling failed (${res.status})`);
          }

          const json: CreateMessageResult = await res.json();

          // 3. Add response to store
          addSamplingResponse(id, server, json);

          // (optional) Return as usual
          return json;
        }
      : undefined;

    const onLogging = async (notif: LoggingMessageNotification) =>
      addNotification(notif);

    const onProgress = async (notif: ProgressNotification) =>
      addProgress(notif);

    const connectServers = async () => {
      await Promise.all(
        activeServers.map((server) =>
          connect(
            clientName ?? "mcphappeditor-web",
            clientVersion ?? "1.0.0",
            server.url,
            server.type,
            {
              headers: server.headers,
              onSample,
              onElicit,
              onLogging,
              onProgress,
            }
          )
        )
      );
    };

    connectServers();

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [JSON.stringify(selected), JSON.stringify(servers), samplingApi]);

  return <>{children}</>;
};
