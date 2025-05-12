import React, { useEffect, useState } from "react";
import Modal from "react-bootstrap/Modal";
import Button from "react-bootstrap/Button";
import Tabs from "react-bootstrap/Tabs";
import Tab from "react-bootstrap/Tab";
import Badge from "react-bootstrap/Badge";
import { McpServerWithName } from "../hooks/useMcpServers";
import { useMcpClientContext } from "../context/McpClientContext";
import MCPServerSummary from "./MCPServerSummary";
import MCPServerPromptsTab from "./MCPServerPromptsTab";
import MCPServerResourcesTab from "./MCPServerResourcesTab";
import MCPServerToolsTab from "./MCPServerToolsTab";

interface MCPServerModalProps {
  show: boolean;
  server: McpServerWithName | null;
  onClose: () => void;
}

type CapabilityState = {
  loading: boolean;
  error: string | null;
  capabilities: any | null;
  prompts: any[] | null;
  promptsLoading: boolean;
  promptsError: string | null;
  resources: any[] | null;
  resourcesLoading: boolean;
  resourcesError: string | null;
  tools: any[] | null;
  toolsLoading: boolean;
  toolsError: string | null;
};

const initialState: CapabilityState = {
  loading: false,
  error: null,
  capabilities: null,
  prompts: null,
  promptsLoading: false,
  promptsError: null,
  resources: null,
  resourcesLoading: false,
  resourcesError: null,
  tools: null,
  toolsLoading: false,
  toolsError: null,
};

const MCPServerModal: React.FC<MCPServerModalProps> = ({ show, server, onClose }) => {
  const { getClient, connected, connect } = useMcpClientContext();
  const [tabKey, setTabKey] = useState<string>("summary");
  const [state, setState] = useState<CapabilityState>(initialState);

  // Reset state on server/modal change
  useEffect(() => {
    setState(initialState);
    setTabKey("summary");
  }, [server, show]);

  // Fetch capabilities and primitive lists
  useEffect(() => {
    if (!server || !show) return;
    let cancelled = false;

    async function fetchAll() {
      setState((s) => ({ ...s, loading: true, error: null }));
      try {
        if (server && !connected[server.name]) {
          await connect(server);
        }
        const client = server ? getClient(server.name) : undefined;
        if (!client) throw new Error("Client not available");
        // Capabilities
        const capabilities = await client.getServerCapabilities();
        if (cancelled) return;
        setState((s) => ({ ...s, loading: false, capabilities }));

        // Prompts
        if (capabilities?.prompts) {
          setState((s) => ({ ...s, promptsLoading: true, promptsError: null }));
          try {
            const { prompts } = await client.listPrompts();
            if (!cancelled) setState((s) => ({ ...s, prompts, promptsLoading: false }));
          } catch (e: any) {
            if (!cancelled) setState((s) => ({ ...s, promptsError: e.message, promptsLoading: false }));
          }
        }

        // Resources
        if (capabilities?.resources) {
          setState((s) => ({ ...s, resourcesLoading: true, resourcesError: null }));
          try {
            const { resources } = await client.listResources();
            if (!cancelled) setState((s) => ({ ...s, resources, resourcesLoading: false }));
          } catch (e: any) {
            if (!cancelled) setState((s) => ({ ...s, resourcesError: e.message, resourcesLoading: false }));
          }
        }

        // Tools
        if (capabilities?.tools) {
          setState((s) => ({ ...s, toolsLoading: true, toolsError: null }));
          try {
            const { tools } = await client.listTools();
            if (!cancelled) setState((s) => ({ ...s, tools, toolsLoading: false }));
          } catch (e: any) {
            if (!cancelled) setState((s) => ({ ...s, toolsError: e.message, toolsLoading: false }));
          }
        }
      } catch (e: any) {
        if (!cancelled) setState((s) => ({ ...s, loading: false, error: e.message }));
      }
    }

    fetchAll();
    return () => { cancelled = true; };
    // eslint-disable-next-line
  }, [server, show]);

  if (!server) return null;

  return (
    <Modal show={show} onHide={onClose} centered size="lg">
      <Modal.Header closeButton>
        <Modal.Title>
          {server?.name} <span style={{ fontWeight: 400, fontSize: 15, color: "#888" }}>Details</span>
        </Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Tabs
          id="mcp-server-modal-tabs"
          activeKey={tabKey}
          onSelect={(k) => setTabKey(k || "summary")}
          className="mb-3"
        >
          <Tab eventKey="summary" title="Summary">
            <MCPServerSummary
              server={server}
              loading={state.loading}
              error={state.error}
              capabilities={state.capabilities}
            />
          </Tab>
          <Tab eventKey="prompts" title={
            <>Prompts {state.capabilities?.prompts && <Badge bg="primary">{state.prompts?.length ?? "…"}</Badge>}</>
          }>
            <MCPServerPromptsTab
              capabilities={state.capabilities}
              prompts={state.prompts}
              loading={state.promptsLoading}
              error={state.promptsError}
            />
          </Tab>
          <Tab eventKey="resources" title={
            <>Resources {state.capabilities?.resources && <Badge bg="success">{state.resources?.length ?? "…"}</Badge>}</>
          }>
            <MCPServerResourcesTab
              capabilities={state.capabilities}
              resources={state.resources}
              loading={state.resourcesLoading}
              error={state.resourcesError}
            />
          </Tab>
          <Tab eventKey="tools" title={
            <>Tools {state.capabilities?.tools && <Badge bg="warning" text="dark">{state.tools?.length ?? "…"}</Badge>}</>
          }>
            <MCPServerToolsTab
              capabilities={state.capabilities}
              tools={state.tools}
              loading={state.toolsLoading}
              error={state.toolsError}
            />
          </Tab>
        </Tabs>
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={onClose}>
          Close
        </Button>
      </Modal.Footer>
    </Modal>
  );
};

export default MCPServerModal;
