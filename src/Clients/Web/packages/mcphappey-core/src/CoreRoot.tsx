// Root component for MCP Happey apps: loads server lists, manages state, renders server list UI.
// Requires a ThemeProvider (throws if missing).

import { useEffect, useState } from "react";
import { useAppStore } from "mcphappey-state";
import { useTheme } from "./ThemeContext";
import ServerList from "./components/ServerList";
import { McpPoolProvider } from "./context/McpPoolContext";

type CoreRootProps = {
  initialLists?: string[];
  allowCustomLists?: boolean;
};

export const CoreRoot = ({
  initialLists = [],
  allowCustomLists = true,
}: CoreRootProps) => {
  //return (<div>Hello from CoreRoot</div>)
  const theme = useTheme(); // Throws if no provider
  const { Alert, Button, Spinner, Input } = theme;
  const { servers, loading, error, importList, clearAll } = useAppStore();
  const [customUrl, setCustomUrl] = useState("");

  // Import initial lists on mount (only once)
  useEffect(() => {
    if (initialLists.length > 0) {
      initialLists.forEach((url) => importList(url));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleAddCustom = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (customUrl.trim()) {
      importList(customUrl.trim());
      setCustomUrl("");
    }
  };

  return (
    <McpPoolProvider>
      <div role="main" style={{ maxWidth: 900, margin: "0 auto", padding: 16 }}>
        <h2>MCP Server Discovery</h2>
        {allowCustomLists && (
          <form
            onSubmit={handleAddCustom}
            style={{ marginBottom: 16, display: "flex", gap: 8 }}
          >
            <Input
              type="url"
              placeholder="Add MCP server list URL"
              value={customUrl}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setCustomUrl(e.target.value)
              }
              required
              style={{ flex: 1 }}
            />
            <Button type="submit" variant="secondary">
              Add
            </Button>
            <Button type="button" variant="outline-danger" onClick={clearAll}>
              Clear
            </Button>
          </form>
        )}
        {loading && (
          <div style={{ margin: "16px 0" }}>
            <Spinner /> Loading server lists...
          </div>
        )}
        {error && (
          <div style={{ margin: "16px 0" }}>
            <Alert variant="danger">{error}</Alert>
          </div>
        )}
        <ServerList servers={servers} />
      </div>
    </McpPoolProvider>
  );
};

export default CoreRoot;
