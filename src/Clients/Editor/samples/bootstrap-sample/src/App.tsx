import CoreRoot from "mcphappeditor-core";
import { ThemeProvider } from "mcphappeditor-theme-bootstrap";
import { loginRequest, msalConfig } from "./msalConfig";
declare const __DEFAULT_MCP_SERVER_LIST_URLS__: string[];
declare const __CHAT_API__: string;
declare const __MODELS_API__: string;
declare const __SAMPLING_API__: string;

const App = () => (
  <ThemeProvider>
    <CoreRoot
      initialMCPLists={__DEFAULT_MCP_SERVER_LIST_URLS__}
      chatConfig={{
        api: __CHAT_API__,
        modelsApi: __MODELS_API__,
        samplingApi: __SAMPLING_API__,
      }}
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
