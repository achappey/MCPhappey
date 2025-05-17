import { useState } from "react";
import { Row, Col, Alert } from "react-bootstrap";
// Removed Spinner, useMcpServers, MCP_SERVERS_API_URL
// Update import path for type
import { McpServerWithName } from "../../types/mcp";
import { useMcpClientContext } from "../../context/McpClientContext";
import MCPServerCard from "../server-card/MCPServerCard";
import MCPServerModal from "../server-modal/MCPServerModal";

// Define props interface
interface MCPServerListProps {
  servers: McpServerWithName[];
}

const MCPServerList = ({ servers }: MCPServerListProps) => {
  // Connection state still comes from context, but the list comes from props
  const {
    connect: contextConnect, // Renaming for clarity if needed
    connecting,
    errors,
  } = useMcpClientContext();

  const [selectedServer, setSelectedServer] = useState<McpServerWithName | null>(
    null
  );
  const [modalOpen, setModalOpen] = useState(false);

  const handleShowDetails = (server: McpServerWithName) => {
    setSelectedServer(server);
    setModalOpen(true);
  };

  const handleCloseModal = () => {
    setModalOpen(false);
    setSelectedServer(null);
  };

  // Loading/Error for the list itself is handled by the parent component (App.tsx)
  // This component now just renders the list it's given.

  if (servers.length === 0) {
    return (
      <Alert variant="info" className="my-3">
        No MCP servers found.
      </Alert>
    );
  }

  return (
    <>
      {/* Global loading/error messages related to OAuth initiation can be handled here if needed,
          or per card. The context now provides `connecting[server.name]` and `errors[server.name]` */}
      <Row xs={1} sm={2} md={3} lg={4} className="g-3">
        {servers.map((server) => (
          <Col key={server.name}>
            <MCPServerCard
              server={server}
              onShowDetails={handleShowDetails}
              // onConnect is now handled by the card itself using context's connect
              // disabled state can be driven by connecting[server.name] from context
            />
          </Col>
        ))}
      </Row>
      <MCPServerModal
        show={modalOpen}
        server={selectedServer}
        onClose={handleCloseModal}
      />
    </>
  );
};

export default MCPServerList;
