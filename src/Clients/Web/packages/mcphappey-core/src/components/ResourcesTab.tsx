import type { McpCapabilitySummary, McpResourceInfo } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import React, { useState, useCallback } from "react";
import ResourceCard from "./ResourceCard";
import ResourceModal from "./ResourceModal";
import { useMcpConnect } from "../hooks/useMcpConnect";

type ResourcesTabProps = {
  serverUrl: string;
  capabilities: McpCapabilitySummary | null | undefined;
  resources: McpResourceInfo[] | null | undefined;
  loading: boolean | undefined;
  error: string | null | undefined;
};

const ResourcesTab = ({
  serverUrl,
  capabilities,
  resources,
  loading,
  error,
}: ResourcesTabProps) => {
  const { Spinner, Alert } = useTheme();
  const { connect } = useMcpConnect();

  const [modalResource, setModalResource] = useState<McpResourceInfo | null>(
    null
  );
  const [resourceContent, setResourceContent] = useState<any>(null);
  const [reading, setReading] = useState(false);
  const [readError, setReadError] = useState<string | null>(null);

  const handleReadResource = useCallback(
    async (res: McpResourceInfo) => {
      setModalResource(res);
      setReading(true);
      setReadError(null);
      setResourceContent(null);
      try {
        const client = await connect(serverUrl);
        const data = await (client as any).readResource({
          uri: res.uri,
        });
        setResourceContent(data);
      } catch (err: any) {
        setReadError(err?.message || String(err));
      } finally {
        setReading(false);
      }
    },
    [connect, serverUrl]
  );

  const handleOpenLink = (res: McpResourceInfo) => {
    window.open(res.uri, "_blank", "noopener");
  };

  const handleCloseModal = () => {
    setModalResource(null);
    setResourceContent(null);
    setReadError(null);
    setReading(false);
  };

  if (!capabilities?.resources) return <span>No resource support.</span>;
  if (loading) return <Spinner size="sm" />;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!resources || resources.length === 0)
    return <span>No resources found.</span>;

  return (
    <>
      <div
        style={{
          display: "grid",
          gap: 16,
        }}
      >
        {resources.map((r) => (
          <div key={r.id}>
            <ResourceCard
              resource={r}
              onRead={handleReadResource}
              onOpenLink={handleOpenLink}
            />
          </div>
        ))}
      </div>
      <ResourceModal
        show={!!modalResource}
        onClose={handleCloseModal}
        resourceName={modalResource?.name || ""}
        mimeType={modalResource?.mimeType}
        content={resourceContent}
        loading={reading}
        error={readError}
      />
    </>
  );
};

export default ResourcesTab;
