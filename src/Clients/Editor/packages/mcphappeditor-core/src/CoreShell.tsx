import React from "react";
import { Outlet } from "react-router";
import { ChatProvider, ChatConfig } from "./components/chat";
import { useTheme } from "./ThemeContext";
import { McpConnectionsProvider } from "./components/mcp/McpConnectionsProvider";
import { ChatAppConnector } from "./components/mcp/ChatAppConnector";
import { I18nProvider } from "mcphappeditor-i18n";
import { ConversationsProvider } from "mcphappeditor-conversations";
import { useEffect } from "react";
import { useRemoteStorageConnected, useAppStore } from "mcphappeditor-state";
import { useAccessToken } from "mcphappeditor-auth/src/msal/useAccessToken";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { useAccount } from "mcphappeditor-auth";

const CONVERSATIONS_SCOPES = ["api://fakton.conversations/AI.use"];

type Props = {
  mcpEditApi?: string;
};

export const CoreShell: React.FC<Props> = ({ mcpEditApi }) => {
  const { Spinner } = useTheme(); // ensure ThemeProvider
  const user = useAccount();
  const remoteStorageConnected = useRemoteStorageConnected();
  const setRemoteStorageConnected = useAppStore(
    (s) => s.setRemoteStorageConnected
  );
  const [, token, error, refresh] = useAccessToken(CONVERSATIONS_SCOPES);
  /*
 
 useEffect(() => {
    console.log(chatConfig);
    setClientName(chatConfig?.appName ?? "web-client");
    // Only run on mount and when remoteStorageConnected changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [chatConfig?.appName]);
*/
  useEffect(() => {
    if (remoteStorageConnected) {
      refresh().finally(() => console.log(token));
      console.log(token);
    }
    // Only run on mount and when remoteStorageConnected changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [remoteStorageConnected]);
  /*
  const ui = mcpEditApi ? (
    <ChatProvider config={chatConfig}>
      <Outlet />
    </ChatProvider>
  ) : (
    <Outlet />
  );*/
  const ui = <div>{mcpEditApi}</div>;
  return user ? <I18nProvider> <Outlet /></I18nProvider> : <Spinner />;
};
