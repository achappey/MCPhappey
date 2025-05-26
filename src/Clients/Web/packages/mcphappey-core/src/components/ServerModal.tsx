import type { McpServerWithName } from "mcphappey-types";
import { useState, useEffect } from "react";
import { useTheme } from "../ThemeContext";
import { useServerPrimitives } from "../hooks/useServerPrimitives";
import ServerSummary from "./ServerSummary";
import PromptsTab from "./PromptsTab";
import ResourcesTab from "./ResourcesTab";
import ToolsTab from "./ToolsTab";

interface ServerModalProps {
  show: boolean;
  server: McpServerWithName | null;
  onClose: () => void;
}

const ServerModal = ({ show, server, onClose }: ServerModalProps) => {
  const { Modal, Tabs, Tab, Button } = useTheme();
  const [tabKey, setTabKey] = useState("summary");
  const primitives = useServerPrimitives(server?.url || null);

  // Reset state on server/modal change
  useEffect(() => {
    setTabKey("summary");
  }, [server, show]);

  // Load all primitives when modal opens
  useEffect(() => {
    if (show && server) {
      primitives.loadAll();
    }
    // eslint-disable-next-line
  }, [show, server?.url]);

  if (!show || !server) return null;

  return (
    <Modal show={show} onHide={onClose} title={server.name} size="lg" centered>
      <div className="mcph-modal-body" style={{ padding: 16 }}>
        <Tabs activeKey={tabKey} onSelect={setTabKey} className="mb-3">
          <Tab eventKey="summary" title="Summary">
            <ServerSummary
              server={server}
              loading={!!primitives.loading}
              toolCount={primitives.tools?.length}
              resourceCount={primitives.resources?.length}
              promptCount={primitives.prompts?.length}
              error={primitives.error}
              capabilities={primitives.capabilities}
            />
          </Tab>
          <Tab eventKey="prompts" title="Prompts">
            <PromptsTab
              serverUrl={server.url}
              capabilities={primitives.capabilities}
              prompts={primitives.prompts}
              loading={!!primitives.loading}
              error={primitives.error}
            />
          </Tab>
          <Tab eventKey="resources" title="Resources">
            <ResourcesTab
              serverUrl={server.url}
              capabilities={primitives.capabilities}
              resources={primitives.resources}
              loading={!!primitives.loading}
              error={primitives.error}
            />
          </Tab>
          <Tab eventKey="tools" title="Tools">
            <ToolsTab
              serverUrl={server.url}
              capabilities={primitives.capabilities}
              tools={primitives.tools}
              loading={!!primitives.loading}
              error={primitives.error}
            />
          </Tab>
        </Tabs>
      </div>
    </Modal>
  );
};

export default ServerModal;
