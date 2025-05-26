import type { McpCapabilitySummary, McpToolInfo } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import React, { useState, useCallback } from "react";
import ToolCard from "./ToolCard";
import ToolModal from "./ToolModal";
import { useMcpConnect } from "../hooks/useMcpConnect";

type ToolsTabProps = {
  serverUrl: string;
  capabilities: McpCapabilitySummary | null | undefined;
  tools: McpToolInfo[] | null | undefined;
  loading: boolean | undefined;
  error: string | null | undefined;
};

const ToolsTab = ({
  serverUrl,
  capabilities,
  tools,
  loading,
  error,
}: ToolsTabProps) => {
  const { Spinner, Alert } = useTheme();
  const { connect } = useMcpConnect();

  const [modalTool, setModalTool] = useState<McpToolInfo | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [toolResult, setToolResult] = useState<any>(null);

  const handleOpenTool = (tool: McpToolInfo) => {
    setModalTool(tool);
    setSubmitError(null);
    setToolResult(null);
  };

  const handleCloseModal = () => {
    setModalTool(null);
    setSubmitError(null);
    setToolResult(null);
  };

  const handleSubmitTool = useCallback(
    async (values: Record<string, any>) => {
      setSubmitError(null);
      try {
        const client = await connect(serverUrl);
        await new Promise((resolve) => setTimeout(resolve, 100));
        if (!modalTool) throw new Error("No tool selected");
        const result = await client.callTool({
          name: modalTool.name,
          arguments: values,
        });
        
        setToolResult(result);
        // eslint-disable-next-line no-console
        console.log("Tool result:", result);
      } catch (err: any) {
        setSubmitError(err?.message || String(err));
      }
    },
    [connect, serverUrl, modalTool]
  );

  if (!capabilities?.tools) return <span>No tool support.</span>;
  if (loading) return <Spinner size="sm" />;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!tools || tools.length === 0) return <span>No tools found.</span>;

  return (
    <>
      {submitError && (
        <Alert variant="danger" className="mb-3">
          {submitError}
        </Alert>
      )}
      <div
        style={{
          display: "grid",
          gap: 16,
        }}
      >
        {tools.map((t) => (
          <div key={t.name}>
            <ToolCard tool={t} onOpen={handleOpenTool} />
          </div>
        ))}
      </div>
      <ToolModal
        show={!!modalTool}
        tool={modalTool}
        onClose={handleCloseModal}
        onSubmit={handleSubmitTool}
        result={toolResult}
      />
    </>
  );
};

export default ToolsTab;
