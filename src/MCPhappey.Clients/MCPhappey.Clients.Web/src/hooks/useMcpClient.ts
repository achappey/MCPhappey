import { useState, useCallback, useEffect } from "react";
import { Client } from "@modelcontextprotocol/sdk/client/index.js"; // Removed McpError
import { StreamableHTTPClientTransport } from "@modelcontextprotocol/sdk/client/streamableHttp.js";
import { getAccessToken, clearAccessToken, initiateOAuthFlow } from '../auth/oauthHandler';

let client: Client | undefined = undefined;
const OAUTH_TARGET_URL_KEY = 'mcp_oauth_target_url'; // Consistent key

export function useMcpClient(url: string) {
  const [connected, setConnected] = useState(false);
  const [isConnecting, setIsConnecting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const connect = useCallback(async (isRetry = false) => {
    if (connected || isConnecting) return;

    setIsConnecting(true);
    setError(null);
    console.log(`Attempting to connect to: ${url}`);

    try {
      const accessToken = getAccessToken(url);
      const headers: Record<string, string> = {};
      if (accessToken) {
        console.log(`Using existing access token for ${url}`);
        headers['Authorization'] = `Bearer ${accessToken}`;
      }

      const baseUrl = new URL(url);
      client = new Client({
        name: "web-client",
        version: "1.0.0",
        headers: headers
      });

      const httpTransport = new StreamableHTTPClientTransport(baseUrl, {
        requestInit: { headers: headers }
      });
      
      await client.connect(httpTransport);

      console.log("Successfully connected to:", url);
      setConnected(true);
      // Clear any OAuth retry flags for this URL if connection is successful
      if (sessionStorage.getItem(OAUTH_TARGET_URL_KEY) === url) {
        sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);
      }

    } catch (err: unknown) {
      console.error(`Connection to ${url} failed:`, err);
      let shouldAttemptOAuth = false;
      let errorMessage = "An unknown connection error occurred.";

      if (err instanceof Error) {
        errorMessage = err.message;
        // Check for 401 indicating conditions. This is speculative and depends on SDK error structure.
        // Prefer specific checks if the SDK provides a way to get status codes.
        if (errorMessage.includes("401") ||
          (err.cause && typeof err.cause === 'object' && err.cause !== null &&
            ((err.cause as any).status === 401 || (err.cause as any).response?.status === 401))) {
          shouldAttemptOAuth = true;
          console.log("Caught 401-like error, attempting OAuth flow.", err);
        } else if (errorMessage.toLowerCase().includes('failed to fetch')) {
          // Generic fetch error might also be a 401, or CORS, or network down.
          console.warn("Caught generic 'Failed to fetch'. If this was a 401, OAuth will be attempted.", err);
          shouldAttemptOAuth = true; // More aggressive: try OAuth on generic fetch failure too
        }
      } else if (typeof err === 'object' && err !== null) {
        // Attempt to get a message if it's an object with a message property
        errorMessage = (err as { message?: string }).message || JSON.stringify(err);
        // Check for status codes if err is an object that might represent an HTTP error response
        if ((err as any).status === 401 || (err as any).response?.status === 401) {
          shouldAttemptOAuth = true;
          console.log("Caught 401-like error object, attempting OAuth flow.", err);
        }
      } else if (typeof err === 'string') {
        errorMessage = err;
        if (err.includes("401")) {
          shouldAttemptOAuth = true;
          console.log("Caught 401-like error string, attempting OAuth flow.", err);
        }
      }

      if (!shouldAttemptOAuth) {
        setError(errorMessage);
      }

      if (shouldAttemptOAuth && !isRetry) { // Avoid OAuth loop if OAuth itself fails to get a good token
        if (getAccessToken(url)) { // If there was a token, it might be stale
          console.log(`Clearing potentially stale token for ${url} and retrying OAuth.`);
          clearAccessToken(url);
        }
        try {
          await initiateOAuthFlow(url);
          // initiateOAuthFlow will redirect. Execution stops here for this path.
          // The app will re-initialize after OAuth callback.
        } catch (oauthError) {
          console.error("OAuth initiation itself failed:", oauthError);
          setError(oauthError instanceof Error ? oauthError.message : "OAuth initiation failed.");
        }
      } else if (shouldAttemptOAuth && isRetry) {
        setError("Connection failed with 401 even after OAuth attempt. Token might be invalid or server requires re-authentication.");
        clearAccessToken(url); // Clear the likely bad token
      }

    } finally {
      setIsConnecting(false);
    }
  }, [url, connected, isConnecting]);

  // Effect to handle auto-connection attempt after OAuth redirect
  useEffect(() => {
    const oauthTargetUrl = sessionStorage.getItem(OAUTH_TARGET_URL_KEY);
    if (oauthTargetUrl === url) {
      console.log(`OAuth callback completed for ${url}, attempting to reconnect.`);
      // Important: Do not remove OAUTH_TARGET_URL_KEY here. 
      // connect() will remove it on successful connection.
      // Pass true to indicate this is a retry post-OAuth.
      connect(true);
    }
  }, [url, connect]);


  const disconnect = useCallback(() => {
    if (client) {
      client.close?.();
      console.log("Disconnected");
      client = undefined;
      setConnected(false);
    }
  }, []);

  return {
    connected,
    connect,
    disconnect,
  };
}
