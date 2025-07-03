// Root component for MCP Happey apps: loads server lists, manages state, renders server list UI.
// Requires a ThemeProvider (throws if missing).
import React, { useEffect, useMemo } from "react";
import { createBrowserRouter, RouterProvider } from "react-router";
import { useTheme } from "./ThemeContext";
import { useRemoteMcpServers } from "./components/mcp/useRemoteMcpServers";
import { CoreShell } from "./CoreShell";
import { ChatPage } from "./components/pages/ChatPage";
import { ServersPage } from "./components/pages/ServersPage";
import { SidebarLayout } from "./components/pages/SidebarLayout";

// --- AUTH INTEGRATION ---
import {
  AuthConfig,
  initAuth,
  acquireAccessToken,
  MsalAuthenticationTemplate,
  InteractionType,
  MsalAuthProvider,
  OAuthCallbackPage,
} from "mcphappeditor-auth";
import { LibraryPage } from "./components/pages/LibraryPage";

type CoreRootProps = {
  appName?: string;
  appVersion?: string;
  mcpEditApi?: string;
  authConfig?: AuthConfig;
};

export const CoreRoot = ({
  mcpEditApi,
  appName,
  appVersion,
  authConfig,
}: CoreRootProps) => {
  useTheme(); // Throws if no provider


  const msalInstance = useMemo(() => {
    if (!authConfig) return null;
    return initAuth(authConfig);
  }, [authConfig]);

  useEffect(() => {
    document.title = appName ?? "AIHappey";
  }, []);

  // 2. merge chatConfig with auth if msal present
  const mergedChatConfig = useMemo(() => {
    if (!authConfig) return mcpEditApi;
    // Ensure api is always present (required by AiChatConfig)
    const api = mcpEditApi ?? "/api";
    return {
      mcpEditApi,
      appName,
      appVersion,
      api,
      getAccessToken: () => acquireAccessToken(authConfig.msal.scopes),
    };
  }, [mcpEditApi, authConfig, appName]);

  // Core routes for internal navigation
  const routes = [
    {
      path: "/oauth-callback",
      element: <OAuthCallbackPage />,
    },
    {
      path: "/*",
      element: (
        <CoreShell
          mcpEditApi={mcpEditApi}
        />
      ),
      children: [
        {
          element: <SidebarLayout />,
          children: [
            { index: true, element: <ServersPage /> },
//            { path: "server/:serverName", element: <ServerEditPage /> },
          ],
        },
      ],
    },
  ];

  const router = createBrowserRouter(routes);
  const routerUi = <RouterProvider router={router} />;

  return msalInstance ? (
    <MsalAuthProvider instance={msalInstance}>
      <MsalAuthenticationTemplate
        interactionType={InteractionType.Redirect}
        authenticationRequest={{ scopes: authConfig!.msal.scopes }}
      >
        {routerUi}
      </MsalAuthenticationTemplate>
    </MsalAuthProvider>
  ) : (
    <>{routerUi}</>
  );
};

export default CoreRoot;
