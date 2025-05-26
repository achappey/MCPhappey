import CoreRoot from "mcphappey-core";
import { ThemeProvider } from "mcphappey-theme-bootstrap";

const App = () => (
  <ThemeProvider>
    <CoreRoot initialLists={["http://localhost:3001/mcp.json"]} />
  </ThemeProvider>
);

export default App;
