// src/auth/OAuthCallbackPage.tsx
import { useEffect, useState } from "react";
import { handleOAuthCallback, saveAccessToken } from "./oauthHandler";
import { useNavigate } from "react-router";

const OAUTH_TARGET_URL_KEY = "mcp_oauth_target_url"; // Same key as in oauthHandler

const OAuthCallbackPage = () => {
  const [status, setStatus] = useState<"pending" | "success" | "error">(
    "pending"
  );
  const [message, setMessage] = useState<string>(
    "Processing authentication callback..."
  );
  const navigate = useNavigate();
  useEffect(() => {
    const processCallback = async () => {
      const result = await handleOAuthCallback();

      if ("error" in result) {
        setStatus("error");
        setMessage(
          `OAuth Error: ${result.error} - ${
            result.errorDescription || "Please try again."
          }`
        );
        // Clean up target URL if auth failed, to prevent loops if user revisits this page.
        sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);
        return;
      }

      if (result.accessToken && result.targetUrl) {
        saveAccessToken(result.targetUrl, result.accessToken);
        setStatus("success");
        setMessage("Authentication successful! Redirecting...");

        // Attempt to redirect to the original target or a sensible default.
        // The main app should ideally pick up the mcp_oauth_target_url
        // from sessionStorage on load if a specific redirection is needed
        // after OAuth to trigger a connection.
        // For now, redirect to home and let the app's main logic handle
        // potential auto-connection if mcp_oauth_target_url was set.

        // Clear the target URL from session storage as it has been processed.
        // This is the primary place this key should be cleared after successful handling.
        console.log("OAuthCallbackPage: Successfully processed callback. Clearing OAUTH_TARGET_URL_KEY.");
        sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);

        // Try to close the window if it was a popup, otherwise redirect.
        // Note: window.close() only works reliably if the window was opened by script.
        // A small delay can sometimes help before attempting to close or redirect.
        setTimeout(async () => {
          // A common pattern is to try to close, and if it fails (e.g. not a popup),
          // then redirect the current tab.
          // However, for simplicity and better UX if it's not a popup,
          // we'll just redirect to the root.
      //    window.location.href = "/"; // Redirect to home page
          await navigate("/")
        }, 1500);
      } else {
        setStatus("error");
        setMessage(
          "OAuth Error: Could not retrieve access token or target URL after callback."
        );
        // Ensure the key is also cleared in this error path if it wasn't already.
        if (sessionStorage.getItem(OAUTH_TARGET_URL_KEY)) {
            console.log("OAuthCallbackPage: Error in processing, but OAUTH_TARGET_URL_KEY was still present. Clearing it.");
            sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);
        }
      }
    };

    processCallback();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [navigate]); // Added navigate to dependency array as it's used in useEffect

  return (
    <div
      style={{ padding: "20px", textAlign: "center", fontFamily: "sans-serif" }}
    >
      <h2>OAuth Authentication Callback</h2>
      <p>{message}</p>
      {status === "pending" && <p>Please wait...</p>}
      {status === "success" && (
        <p>
          If you are not redirected automatically, please{" "}
          <a href="/">click here to return to the application</a>.
        </p>
      )}
      {status === "error" && (
        <p>
          Please <a href="/">return to the application</a> and try again.
        </p>
      )}
    </div>
  );
};

export default OAuthCallbackPage;
