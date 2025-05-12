// McpClientContext.tsx
import {
  createContext,
  useContext,
  useState,
  useCallback,
  useMemo,
  useEffect,
} from "react";
import { useMcpServers, McpServerWithName } from "../hooks/useMcpServers";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StreamableHTTPClientTransport } from "@modelcontextprotocol/sdk/client/streamableHttp.js";
import { MCP_SERVERS_API_URL } from "../config";
import {
  getAccessToken,
  clearAccessToken,
  initiateOAuthFlow,
} from "../auth/oauthHandler";

const OAUTH_TARGET_URL_KEY = "mcp_oauth_target_url"; // Consistent key
const MCP_ACTIVE_CONNECTIONS_KEY = 'mcp_active_connections';

interface McpClientContextValue {
  servers: McpServerWithName[];
  clients: { [serverName: string]: Client | undefined };
  connected: { [serverName: string]: boolean };
  connecting: { [serverName: string]: boolean }; // To show loading state per server
  errors: { [serverName: string]: string | null }; // To show error state per server
  connect: (server: McpServerWithName, isRetry?: boolean) => Promise<void>;
  disconnect: (server: McpServerWithName) => void;
  getClient: (serverName: string) => Client | undefined;
}

const McpClientContext = createContext<McpClientContextValue | undefined>(
  undefined
);

export const McpClientProvider = ({
  children,
}: {
  children: React.ReactNode;
}) => {
  const { servers } = useMcpServers(MCP_SERVERS_API_URL);
  const [clients, setClients] = useState<{
    [serverName: string]: Client | undefined;
  }>({});
  const [connected, setConnected] = useState<{ [serverName: string]: boolean }>(
    {}
  );
  const [connecting, setConnecting] = useState<{
    [serverName: string]: boolean;
  }>({});
  const [errors, setErrors] = useState<{ [serverName: string]: string | null }>(
    {}
  );

  const connect = useCallback(
    async (server: McpServerWithName, isRetry = false) => {
      if (connected[server.name] || connecting[server.name]) return;

      setConnecting((prev) => ({ ...prev, [server.name]: true }));
      setErrors((prev) => ({ ...prev, [server.name]: null }));
      console.log(`Context: Attempting to connect to: ${server.url}`);

      try {
        const accessToken = getAccessToken(server.url);
        const headers: Record<string, string> = {};
        if (accessToken) {
          console.log(`Context: Using existing access token for ${server.url}`);
          headers["Authorization"] = `Bearer ${accessToken}`;
        }

        const baseUrl = new URL(server.url);
        const client = new Client({
          name: "web-client",
          version: "1.0.0",
          //headers: headers,
        });

        const httpTransport = new StreamableHTTPClientTransport(baseUrl, {
          requestInit: {
            headers: headers,
          },
        });
        
        await client.connect(httpTransport);

        console.log(`Context: Successfully connected to: ${server.url}`);
        setClients((prev) => ({ ...prev, [server.name]: client }));
        setConnected((prev) => ({ ...prev, [server.name]: true }));

        // Persist this successful connection attempt
        try {
          const activeConnectionsRaw = localStorage.getItem(MCP_ACTIVE_CONNECTIONS_KEY);
          let activeConnections: string[] = activeConnectionsRaw ? JSON.parse(activeConnectionsRaw) : [];
          if (!activeConnections.includes(server.url)) {
            activeConnections.push(server.url);
            localStorage.setItem(MCP_ACTIVE_CONNECTIONS_KEY, JSON.stringify(activeConnections));
          }
        } catch (e) {
          console.error("Failed to update active connections in localStorage on connect:", e);
        }

        if (sessionStorage.getItem(OAUTH_TARGET_URL_KEY) === server.url) {
          sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);
        }
      } catch (err: unknown) {
        console.error(`Context: Connection to ${server.url} failed:`, err);
        let shouldAttemptOAuth = false;
        let errorMessage = "An unknown connection error occurred.";

        if (err instanceof Error) {
          errorMessage = err.message;
          if (
            errorMessage.includes("401") ||
            (err.cause &&
              typeof err.cause === "object" &&
              err.cause !== null &&
              ((err.cause as any).status === 401 ||
                (err.cause as any).response?.status === 401))
          ) {
            shouldAttemptOAuth = true;
            console.log(
              "Context: Caught 401-like error, attempting OAuth flow.",
              err
            );
          } else if (errorMessage.toLowerCase().includes("failed to fetch")) {
            console.warn(
              "Context: Caught generic 'Failed to fetch'. If this was a 401, OAuth will be attempted.",
              err
            );
            shouldAttemptOAuth = true;
          }
        } else if (typeof err === "object" && err !== null) {
          errorMessage =
            (err as { message?: string }).message || JSON.stringify(err);
          if (
            (err as any).status === 401 ||
            (err as any).response?.status === 401
          ) {
            shouldAttemptOAuth = true;
            console.log(
              "Context: Caught 401-like error object, attempting OAuth flow.",
              err
            );
          }
        } else if (typeof err === "string") {
          errorMessage = err;
          if (err.includes("401")) {
            shouldAttemptOAuth = true;
            console.log(
              "Context: Caught 401-like error string, attempting OAuth flow.",
              err
            );
          }
        }

        if (!shouldAttemptOAuth) {
          setErrors((prev) => ({ ...prev, [server.name]: errorMessage }));
        }

        if (shouldAttemptOAuth && !isRetry) {
          if (getAccessToken(server.url)) {
            console.log(
              `Context: Clearing potentially stale token for ${server.url} and retrying OAuth.`
            );
            clearAccessToken(server.url);
          }
          try {
            // Store the server name or URL to identify which connection triggered OAuth
            // initiateOAuthFlow already stores OAUTH_TARGET_URL_KEY with server.url
            await initiateOAuthFlow(server.url);
            // Redirect happens here, so no further state updates in this execution path.
          } catch (oauthError) {
            console.error(
              "Context: OAuth initiation itself failed:",
              oauthError
            );
            const oauthErrMessage =
              oauthError instanceof Error
                ? oauthError.message
                : "OAuth initiation failed.";
            setErrors((prev) => ({ ...prev, [server.name]: oauthErrMessage }));
          }
        } else if (shouldAttemptOAuth && isRetry) {
          const retryErrorMessage =
            "Connection failed with 401 even after OAuth attempt. Token might be invalid or server requires re-authentication.";
          setErrors((prev) => ({ ...prev, [server.name]: retryErrorMessage }));
          clearAccessToken(server.url);
        }
      } finally {
        setConnecting((prev) => ({ ...prev, [server.name]: false }));
      }
    },
    [connected, connecting]
  );

  // Effect to handle auto-connection attempts after OAuth redirect
  // This iterates through known servers to see if any match the OAuth target
  useEffect(() => {
    const oauthTargetUrl = sessionStorage.getItem(OAUTH_TARGET_URL_KEY);
    if (oauthTargetUrl) {
      const serverToRetry = servers.find((s) => s.url === oauthTargetUrl);
      if (serverToRetry) { // Found a server matching the OAuth target URL
        // Attempt connection only if not already connected or connecting
        if (!connected[serverToRetry.name] && !connecting[serverToRetry.name]) {
          console.log(
            `Context: OAuth callback completed for ${oauthTargetUrl}, attempting to reconnect server ${serverToRetry.name}.`
          );
          connect(serverToRetry, true); // Pass true for isRetry
        }
        // DO NOT REMOVE THE KEY HERE. OAuthCallbackPage is responsible for that.
        // If serverToRetry was not found, or already connected/connecting, the key remains
        // for OAuthCallbackPage to potentially clear if it handles an error state,
        // or if the user navigates away from /oauth-callback before it processes.
        // OAuthCallbackPage should be the primary clearer of this key upon its own successful processing or error handling.
      }
      // If oauthTargetUrl is present but serverToRetry is not found (e.g. server list changed),
      // the key might remain. This is less ideal but OAuthCallbackPage should still clear it.
      // A more robust solution might involve OAuthCallbackPage signaling completion differently,
      // but for now, deferring removal to OAuthCallbackPage is safer to prevent premature deletion.
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [servers, connected, connecting]); // connect is memoized, should be stable if its own deps are.

  // Effect for auto-reconnecting on application load
  useEffect(() => {
    if (servers.length > 0) { // Ensure servers list from useMcpServers is populated
      const activeConnectionsRaw = localStorage.getItem(MCP_ACTIVE_CONNECTIONS_KEY);
      if (activeConnectionsRaw) {
        try {
          const activeConnectionUrls = JSON.parse(activeConnectionsRaw) as string[];
          activeConnectionUrls.forEach(url => {
            const serverToReconnect = servers.find(s => s.url === url);
            if (serverToReconnect && !connected[serverToReconnect.name] && !connecting[serverToReconnect.name]) {
              console.log(`Context: Auto-reconnecting to ${url} for server ${serverToReconnect.name}`);
              connect(serverToReconnect); // Standard connect attempt
            }
          });
        } catch (e) {
          console.error("Failed to parse or process active connections from localStorage for auto-reconnect:", e);
          localStorage.removeItem(MCP_ACTIVE_CONNECTIONS_KEY); // Clear potentially corrupted data
        }
      }
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [servers]); // Rerun when the list of servers is available/changes.

  const disconnect = useCallback(
    (server: McpServerWithName) => {
      const client = clients[server.name];
      if (client) {
        client.close?.();
      }
      clearAccessToken(server.url); // Clear token on disconnect
      try {
        const activeConnectionsRaw = localStorage.getItem(MCP_ACTIVE_CONNECTIONS_KEY);
        if (activeConnectionsRaw) {
          let activeConnections: string[] = JSON.parse(activeConnectionsRaw);
          activeConnections = activeConnections.filter((url: string) => url !== server.url);
          localStorage.setItem(MCP_ACTIVE_CONNECTIONS_KEY, JSON.stringify(activeConnections));
        }
      } catch (e) {
        console.error("Failed to update active connections in localStorage on disconnect:", e);
      }

      setClients((prev) => ({ ...prev, [server.name]: undefined }));
      setConnected((prev) => ({ ...prev, [server.name]: false }));
      setConnecting((prev) => ({ ...prev, [server.name]: false }));
      setErrors((prev) => ({ ...prev, [server.name]: null }));
    },
    [clients] // clients is a dependency for accessing the specific client instance
  );

  const getClient = useCallback(
    (serverName: string) => clients[serverName],
    [clients]
  );

  const value = useMemo(
    () => ({
      servers,
      clients,
      connected,
      connecting, // expose connecting state
      errors, // expose error state
      connect,
      disconnect,
      getClient,
    }),
    [
      servers,
      clients,
      connected,
      connecting,
      errors,
      connect,
      disconnect,
      getClient,
    ]
  );

  return (
    <McpClientContext.Provider value={value}>
      {children}
    </McpClientContext.Provider>
  );
};

export function useMcpClientContext() {
  const ctx = useContext(McpClientContext);
  if (!ctx)
    throw new Error(
      "useMcpClientContext must be used within a McpClientProvider"
    );
  return ctx;
}
