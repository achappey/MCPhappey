import React from "react";
import Container from "react-bootstrap/Container";
import MCPServerList from "./components/MCPServerList";
import { McpClientProvider } from "./context/McpClientContext";

const App: React.FC = () => {
  return (
    <McpClientProvider>
      <Container style={{ marginTop: 40 }}>
        <h1 className="mb-4" style={{ textAlign: "center" }}>
          MCP Servers
        </h1>
        <MCPServerList />
      </Container>
    </McpClientProvider>
  );
};

export default App;
