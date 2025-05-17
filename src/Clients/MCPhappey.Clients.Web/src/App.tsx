import { useState, useEffect } from "react";
import {
  Container,
  Tabs,
  Tab,
  Form,
  Button,
  Spinner,
  Alert,
  Row,
  Col,
  InputGroup,
  CloseButton,
} from "react-bootstrap";
import MCPServerList from "./features/server-list/MCPServerList";
import { McpClientProvider } from "./context/McpClientContext";
import { useServerLists } from "./hooks/useServerLists";
import { useMcpServerData } from "./hooks/useMcpServerData";
import { McpServerWithName } from "./types/mcp"; // Import the type
// DEFAULT_MCP_SERVER_LIST_URLS is no longer needed here directly, but keep for reference if needed

const App = () => {
  const { allUrls, customUrls, addUrl, removeUrl } = useServerLists();
  const { serverData } = useMcpServerData(allUrls);
  const [activeKey, setActiveKey] = useState<string | null>(null);
  const [newUrlInput, setNewUrlInput] = useState("");

  // Calculate the consolidated list of all servers from all fetched lists
  const allServers: McpServerWithName[] = Object.values(serverData)
    .flatMap((data) => data?.servers || [])
    // Ensure uniqueness based on server name AND url (in case same name appears in different lists)
    .filter(
      (server, index, self) =>
        index ===
        self.findIndex((s) => s.name === server.name && s.url === server.url)
    );

  // Set the initial active tab when URLs load
  useEffect(() => {
    if (!activeKey && allUrls.length > 0) {
      setActiveKey(allUrls[0]);
    } else if (activeKey && !allUrls.includes(activeKey)) {
      // If the active tab was removed, switch to the first available one
      setActiveKey(allUrls.length > 0 ? allUrls[0] : null);
    }
  }, [allUrls, activeKey]);

  const handleAddUrl = (e: React.FormEvent) => {
    e.preventDefault();
    if (newUrlInput.trim()) {
      try {
        // Basic URL validation
        new URL(newUrlInput.trim());
        addUrl(newUrlInput.trim());
        setNewUrlInput("");
      } catch (error) {
        alert("Invalid URL format."); // Simple validation feedback
      }
    }
  };

  const getTabTitle = (url: string) => {
    try {
      const parsedUrl = new URL(url);
      // Use hostname or a relevant part of the path if hostname is generic (like localhost)
      return parsedUrl.hostname === "localhost" ||
        parsedUrl.hostname === "127.0.0.1"
        ? `${parsedUrl.hostname}:${parsedUrl.port}`
        : parsedUrl.hostname;
    } catch {
      return url; // Fallback to full URL if parsing fails
    }
  };

  return (
    // Pass the consolidated allServers list to the provider
    <McpClientProvider allServers={allServers}>
      <Container className="mt-4">
        <h1 className="mb-4 text-center">MCP Server Discovery</h1>

        {/* Add New URL Form */}
        <Form onSubmit={handleAddUrl} className="mb-4">
          <Row>
            <Col>
              <InputGroup>
                <Form.Control
                  type="url"
                  placeholder="Enter custom MCP server list URL (e.g., http://.../mcp.json)"
                  value={newUrlInput}
                  onChange={(e) => setNewUrlInput(e.target.value)}
                  required
                />
                <Button variant="outline-secondary" type="submit">
                  Add List
                </Button>
              </InputGroup>
            </Col>
          </Row>
        </Form>

        {/* Server List Tabs */}
        {allUrls.length > 0 && activeKey ? (
          <Tabs
            activeKey={activeKey}
            onSelect={(k) => setActiveKey(k)}
            id="server-list-tabs"
            className="mb-3"
            mountOnEnter // Mount tab content only when active
            unmountOnExit={false} // Keep content mounted after first activation
          >
            {allUrls.map((url) => {
              const data = serverData[url];
              const isCustom = customUrls.includes(url);
              const title = (
                <>
                  {getTabTitle(url)}
                  {isCustom && (
                    <CloseButton
                      onClick={(e) => {
                        e.stopPropagation(); // Prevent tab selection
                        if (window.confirm(`Remove list: ${url}?`)) {
                          removeUrl(url);
                        }
                      }}
                      className="ms-2"
                      style={{ fontSize: "0.6rem", verticalAlign: "middle" }}
                      aria-label="Remove custom list"
                    />
                  )}
                </>
              );

              return (
                <Tab eventKey={url} title={title} key={url}>
                  {data?.loading && (
                    <div className="text-center p-5">
                      <Spinner animation="border" role="status">
                        <span className="visually-hidden">
                          Loading servers...
                        </span>
                      </Spinner>
                    </div>
                  )}
                  {data?.error && (
                    <Alert variant="danger">
                      Error loading servers from {url}: {data.error}
                    </Alert>
                  )}
                  {!data?.loading && !data?.error && (
                    // Pass servers prop here - MCPServerList needs update
                    <MCPServerList servers={data?.servers || []} />
                  )}
                </Tab>
              );
            })}
          </Tabs>
        ) : (
          <Alert variant="info">
            No server lists configured. Add a custom URL above.
          </Alert>
        )}
      </Container>
    </McpClientProvider>
  );
};

export default App;
