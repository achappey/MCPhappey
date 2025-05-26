import CoreRoot from "mcphappey-core";
import { ThemeProvider } from "mcphappey-theme-fluent";
import { DEFAULT_MCP_SERVER_LIST_URLS } from "./config";

const App = () => (
  <ThemeProvider>
    <CoreRoot initialLists={DEFAULT_MCP_SERVER_LIST_URLS} />
  </ThemeProvider>
);

export default App;
