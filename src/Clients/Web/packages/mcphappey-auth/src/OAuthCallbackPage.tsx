import { useEffect, useState } from "react";
import { handleOAuthCallback } from "./oauthFlow";
import { saveAccessToken } from "./storage";
import { useNavigate } from "react-router";

const OAUTH_TARGET_URL_KEY = "mcp_oauth_target_url";

const OAuthCallbackPage = () => {
  const [status, setStatus] = useState<"pending" | "success" | "error">("pending");
  const [message, setMessage] = useState<string>("Processing authentication callback...");
  const navigate = useNavigate();

  useEffect(() => {
    const processCallback = async () => {
      const result = await handleOAuthCallback();

      if ("error" in result) {
        setStatus("error");
        setMessage(
          `OAuth Error: ${result.error} - ${result.errorDescription || "Please try again."}`
        );
        sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);
        return;
      }

      if (result.accessToken && result.targetUrl) {
        saveAccessToken(result.targetUrl, result.accessToken);
        setStatus("success");
        setMessage("Authentication successful! Redirecting...");
        sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);

        setTimeout(async () => {
          await navigate("/");
        }, 1500);
      } else {
        setStatus("error");
        setMessage(
          "OAuth Error: Could not retrieve access token or target URL after callback."
        );
        if (sessionStorage.getItem(OAUTH_TARGET_URL_KEY)) {
          sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);
        }
      }
    };

    processCallback();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [navigate]);

  return (
    <div style={{ padding: 20, textAlign: "center", fontFamily: "sans-serif" }}>
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
