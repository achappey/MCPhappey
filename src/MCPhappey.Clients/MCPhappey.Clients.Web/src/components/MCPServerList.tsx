import React, { useState } from "react";
import Row from "react-bootstrap/Row";
import Col from "react-bootstrap/Col";
import Alert from "react-bootstrap/Alert";
import Spinner from "react-bootstrap/Spinner";
import { useMcpServers, McpServerWithName } from "../hooks/useMcpServers"; // This hook might need adjustment if it provides servers directly without context
import { useMcpClientContext } from "../context/McpClientContext";
import { MCP_SERVERS_API_URL } from "../config";
import MCPServerCard from "./MCPServerCard";
import MCPServerModal from "./MCPServerModal";

// MCP_SERVERS_API_URL is now likely managed within McpClientProvider or useMcpServers if it fetches them.
// If useMcpServers is just a static list provider, it's fine.

const MCPServerList: React.FC = () => {
  // Servers, connecting, and errors will now come from the context
  const { 
    servers, 
    connect: contextConnect, // Renaming to avoid conflict if we had a local connect
    connecting, 
    errors 
  } = useMcpClientContext();
  
  // useMcpServers might still be used if it's just fetching the list of available servers,
  // but the actual connection state comes from McpClientContext.
  // For this example, I'll assume `servers` from context is the primary source.
  // If `useMcpServers` has its own loading/error for fetching the *list* of servers, that's separate.
  // Let's assume for now `useMcpServers` is primarily for fetching the server list for the context to use.
  // The local loading/error state here would be for the list fetching itself, not connection.
  const { loading: serverListLoading, error: serverListError } = useMcpServers(MCP_SERVERS_API_URL);


  const [selectedServer, setSelectedServer] =
    useState<McpServerWithName | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  const handleShowDetails = (server: McpServerWithName) => {
    setSelectedServer(server);
    setModalOpen(true);
  };

  const handleCloseModal = () => {
    setModalOpen(false);
    setSelectedServer(null);
  };

  // The connect action is now handled by MCPServerCard via context
  // So, handleConnect in this component is no longer strictly necessary
  // unless it needs to do something before or after the context's connect.
  // For now, we'll let MCPServerCard trigger connect directly.

  if (serverListLoading) {
    return (
      <div className="d-flex justify-content-center my-5">
        <Spinner animation="border" role="status" />
      </div>
    );
  }

  if (serverListError) {
    return (
      <Alert variant="danger" className="my-3">
        Error loading server list: {serverListError}
      </Alert>
    );
  }

  if (!serverListLoading && servers.length === 0) {
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
