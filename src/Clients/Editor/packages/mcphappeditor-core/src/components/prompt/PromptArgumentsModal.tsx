import { useState } from "react";
import { Prompt } from "mcphappeditor-mcp";
import { useTheme } from "../../ThemeContext";
import { CancelButton } from "../common/CancelButton";
import { useAppStore } from "mcphappeditor-state";
import { toMarkdownLinkSmart } from "../chat/utils/markdown";
import { useEnqueueUserMessage } from "../chat/hooks/useEnqueueUserMessage";

/**
 * Extends prompt with originating server URL so we can reach the correct client.
 */
export type PromptWithSource = Prompt & { _url: string };

type Props = {
  prompt: PromptWithSource;
  onHide: () => void;
  model?: string;
};

/**
 * Renders a form for prompt.arguments, calls getPrompt, then appends the
 * returned messages to the chat store.
 */
export const PromptArgumentsModal = ({ prompt, onHide, model }: Props) => {
  const { Modal, Button, Input, Spinner, Alert } = useTheme();

  // Build initial form state with empty strings
  const initialValues = Object.fromEntries(
    (prompt.arguments ?? []).map((a) => [a.name, ""])
  ) as Record<string, string>;

  const [values, setValues] = useState<Record<string, string>>(initialValues);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const clients = useAppStore((a) => a.clients);
  const client = clients?.[prompt._url];
  const enqueue = useEnqueueUserMessage();
  const appendPromptMessages = async (messages: any[]) => {
    const parts: any[] = messages.map((m) => ({
      type: "text",
      text:
        m.content.text ??
        toMarkdownLinkSmart(
          m.content.resource.uri,
          m.content.resource.text as string,
          m.content.resource.mimeType
        ),
    }));
    await enqueue(parts, model);
  };

  // Validation: all required fields non-empty
  const missingRequired = (prompt.arguments ?? []).some(
    (a) => a.required && !values[a.name]?.trim()
  );

  // Event handlers -----------------------------------------------------------
  const handleChange = (name: string, value: string) =>
    setValues((v) => ({ ...v, [name]: value }));

  const handleOk = async () => {
    if (missingRequired || pending) return;
    if (!client || typeof (client as any).getPrompt !== "function") {
      setError("Server client not available.");
      return;
    }

    setPending(true);
    setError(null);
    try {
      const result = await (client as any).getPrompt({
        name: prompt.name,
        arguments: values,
      });
      onHide();
      await appendPromptMessages(result.messages ?? []);
    } catch (err: any) {
      setError(err?.message ?? String(err));
    } finally {
      setPending(false);
    }
  };

  // UI -----------------------------------------------------------------------
  return (
    <Modal
      show={true}
      onHide={onHide}
      actions={
        <>
          <CancelButton disabled={pending} onClick={onHide} />
          <Button
            type="button"
            onClick={handleOk}
            disabled={pending || missingRequired}
          >
            {pending ? <Spinner size="sm" /> : "OK"}
          </Button>
        </>
      }
      title={`Fill arguments – ${prompt.name}`}
    >
      <div style={{ minWidth: 320, maxHeight: 400, overflowY: "auto" }}>
        {(prompt.arguments ?? []).map((arg) => (
          <div key={arg.name} style={{ marginBottom: 12 }}>
            <label
              style={{ display: "block", fontWeight: 500, marginBottom: 4 }}
            >
              {arg.name}
              {arg.required && <span style={{ color: "red" }}> *</span>}
            </label>
            {arg.description && (
              <div style={{ fontSize: 12, color: "#666", marginBottom: 4 }}>
                {arg.description}
              </div>
            )}
            <Input
              value={values[arg.name] ?? ""}
              onChange={(e: any) => handleChange(arg.name, e.target.value)}
              disabled={pending}
            />
          </div>
        ))}
        {error && (
          <div style={{ marginTop: 4 }}>
            <Alert variant="danger">{error}</Alert>
          </div>
        )}
      </div>
    </Modal>
  );
};
