import CoreRoot from "mcphappeditor-core";
import { ThemeProvider } from "mcphappeditor-theme-fluent";
import { loginRequest, msalConfig } from "./msalConfig";
declare const __MCP_EDIT_API__: string;
declare const __APP_NAME__: string;
declare const __APP_VERSION__: string;

const App = () => (
  <ThemeProvider>
    <CoreRoot
      appName={__APP_NAME__}
      appVersion={__APP_VERSION__}
      mcpEditApi={__MCP_EDIT_API__}
      authConfig={{
        msal: {
          clientId: msalConfig.auth.clientId,
          authority: msalConfig.auth.authority!,
          redirectUri: msalConfig.auth.redirectUri!,
          scopes: loginRequest.scopes!,
        },
      }}
    />
  </ThemeProvider>
);

export default App;
