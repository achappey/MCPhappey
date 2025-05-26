import type { McpCapabilitySummary, McpPromptInfo } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import PromptCard from "./PromptCard";
import PromptModal from "./PromptModal";
import React, { useState } from "react";

type PromptsTabProps = {
  serverUrl: string;
  capabilities: McpCapabilitySummary | null | undefined;
  prompts: McpPromptInfo[] | null | undefined;
  loading: boolean | undefined;
  error: string | null | undefined;
};

import { useMcpConnect } from "../hooks/useMcpConnect";
import { useCallback } from "react";

const PromptsTab = ({
  serverUrl,
  capabilities,
  prompts,
  loading,
  error,
}: PromptsTabProps) => {
  const { Spinner, Alert } = useTheme();
  const [modalPrompt, setModalPrompt] = useState<McpPromptInfo | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [promptResult, setPromptResult] = useState<any>(null);
  const { connect } = useMcpConnect();

  const handleOpenPrompt = (prompt: McpPromptInfo) => {
    setModalPrompt(prompt);
    setSubmitError(null);
    setPromptResult(null);
  };

  const handleCloseModal = () => {
    setModalPrompt(null);
    setSubmitError(null);
    setPromptResult(null);
  };

  const handleSubmitPrompt = useCallback(
    async (values: Record<string, string>) => {
      setSubmitError(null);
      try {
        const client = await connect(serverUrl);
        if (!modalPrompt) throw new Error("No prompt selected");
        const result = await client.getPrompt({
          name: modalPrompt.name,
          arguments: values,
        });
        setPromptResult(result);
        // eslint-disable-next-line no-console
        console.log("Prompt result:", result);
      //  setModalPrompt(null);
      } catch (err: any) {
        setSubmitError(err?.message || String(err));
      }
    },
    [connect, serverUrl, modalPrompt]
  );

  if (!capabilities?.prompts) return <span>No prompt support.</span>;
  if (loading) return <Spinner size="sm" />;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!prompts || prompts.length === 0) return <span>No prompts found.</span>;

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
        {prompts.map((p) => (
          <div key={p.name}>
            <PromptCard prompt={p} onOpen={handleOpenPrompt} />
          </div>
        ))}
      </div>
      <PromptModal
        show={!!modalPrompt}
        prompt={modalPrompt}
        onClose={handleCloseModal}
        onSubmit={handleSubmitPrompt}
        result={promptResult}
      />
    </>
  );
};

export default PromptsTab;
