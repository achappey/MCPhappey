import { useEffect } from "react";
import { useAppStore } from "mcphappeditor-state";
import { acquireAccessToken } from "mcphappeditor-auth";
import { CreateMessageRequest } from "mcphappeditor-mcp";

/**
 * ChatAppConnector - Connects the chat-app MCP client on mount.
 * Optionally supports sampling via samplingApi.
 */
export const ChatAppConnector = ({
  clientName = "mcphappeditor-web",
  clientVersion = "1.0.0",
  mcpUrl,
  samplingApi,
}: {
  clientName?: string;
  clientVersion?: string;
  mcpUrl?: string;
  samplingApi?: string;
}) => {
  const connectChatApp = useAppStore((s) => s.connectChatApp);
  const chatAppMcp = useAppStore((s) => s.chatAppMcp);

  useEffect(() => {
    // Build onSample callback if samplingApi is provided
    const onSample = samplingApi
      ? async (server: string, params: CreateMessageRequest) => {
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
          return await res.json();
        }
      : undefined;

    //if (!chatAppMcp) {
    if (mcpUrl) {
      connectChatApp(clientName, clientVersion, mcpUrl, { onSample });
    } else {
      if (typeof window !== "undefined") {
        // eslint-disable-next-line no-console
        console.warn(
          "CHAT_MCP_URL not set; Chat-App MCP client will not connect."
        );
      }
    }
    // }
    // Only run on mount or when chatAppMcp changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [samplingApi, mcpUrl, clientName, clientVersion]);

  return null;
};
