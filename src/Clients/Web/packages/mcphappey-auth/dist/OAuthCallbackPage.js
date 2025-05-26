import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { handleOAuthCallback } from "./oauthFlow";
import { saveAccessToken } from "./storage";
import { useNavigate } from "react-router";
const OAUTH_TARGET_URL_KEY = "mcp_oauth_target_url";
const OAuthCallbackPage = () => {
    const [status, setStatus] = useState("pending");
    const [message, setMessage] = useState("Processing authentication callback...");
    const navigate = useNavigate();
    useEffect(() => {
        const processCallback = async () => {
            const result = await handleOAuthCallback();
            if ("error" in result) {
                setStatus("error");
                setMessage(`OAuth Error: ${result.error} - ${result.errorDescription || "Please try again."}`);
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
            }
            else {
                setStatus("error");
                setMessage("OAuth Error: Could not retrieve access token or target URL after callback.");
                if (sessionStorage.getItem(OAUTH_TARGET_URL_KEY)) {
                    sessionStorage.removeItem(OAUTH_TARGET_URL_KEY);
                }
            }
        };
        processCallback();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [navigate]);
    return (_jsxs("div", { style: { padding: 20, textAlign: "center", fontFamily: "sans-serif" }, children: [_jsx("h2", { children: "OAuth Authentication Callback" }), _jsx("p", { children: message }), status === "pending" && _jsx("p", { children: "Please wait..." }), status === "success" && (_jsxs("p", { children: ["If you are not redirected automatically, please", " ", _jsx("a", { href: "/", children: "click here to return to the application" }), "."] })), status === "error" && (_jsxs("p", { children: ["Please ", _jsx("a", { href: "/", children: "return to the application" }), " and try again."] }))] }));
};
export default OAuthCallbackPage;
//# sourceMappingURL=OAuthCallbackPage.js.map