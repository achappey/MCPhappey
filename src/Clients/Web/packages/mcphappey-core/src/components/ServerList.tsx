import { useState } from "react";
import type { McpServerWithName } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import ServerCard from "./ServerCard";
import ServerModal from "./ServerModal";
import { getAccessToken } from "mcphappey-auth";

// Props interface
interface ServerListProps {
  servers: McpServerWithName[];
}

const ServerList = ({ servers }: ServerListProps) => {
  const { Alert, Switch, TextArea } = useTheme();
  const [selectedServer, setSelectedServer] =
    useState<McpServerWithName | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  // --- ADDED: toggle state
  const [showRaw, setShowRaw] = useState(false);

  const handleShowDetails = (server: McpServerWithName) => {
    setSelectedServer(server);
    setModalOpen(true);
  };

  const handleCloseModal = () => {
    setModalOpen(false);
    setSelectedServer(null);
  };

  // --- ADDED: augment servers with auth header if token present
  const serversWithAuth = servers.map((s) => {
    const token = getAccessToken(s.url);
    return token
      ? {
          ...s,
          headers: { ...(s.headers ?? {}), Authorization: `Bearer ${token}` },
        }
      : s;
  });

  const serverDict = Object.fromEntries(
    serversWithAuth.map(({ name, ...rest }) => [name, rest])
  );

  if (servers.length === 0) {
    return (
      <Alert variant="info" className="my-3">
        No MCP servers found.
      </Alert>
    );
  }

  return (
    <>
      {/* --- ADDED: Toggle for raw JSON view */}
      <div style={{ marginBottom: 16 }}>
        <Switch
          id="raw-json-toggle"
          label="Show raw JSON"
          checked={showRaw}
          onChange={setShowRaw}
        />
      </div>
      {showRaw ? (
        <TextArea
          rows={Math.min(20, serversWithAuth.length * 4)}
          value={JSON.stringify({ servers: serverDict }, null, 2)}
          style={{
            fontFamily: "monospace",
            fontSize: 12,
            background: "#f8f9fa",
            borderRadius: 4,
          }}
          onChange={() => {}}
        />
      ) : (
        <>
          <div style={{ display: "flex", flexWrap: "wrap", gap: 16 }}>
            {servers.map((server) => (
              <div
                key={server.name}
                style={{ flex: "1 0 250px", minWidth: 250 }}
              >
                <ServerCard server={server} onShowDetails={handleShowDetails} />
              </div>
            ))}
          </div>
          <ServerModal
            show={modalOpen}
            server={selectedServer}
            onClose={handleCloseModal}
          />
        </>
      )}
    </>
  );
};

export default ServerList;
