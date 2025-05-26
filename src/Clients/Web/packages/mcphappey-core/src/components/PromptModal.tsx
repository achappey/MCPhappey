import React, { useState, useEffect } from "react";
import type { McpPromptInfo, McpPromptArgument } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import { JsonView, defaultStyles } from "react-json-view-lite";
import "react-json-view-lite/dist/index.css";

type PromptModalProps = {
  show: boolean;
  prompt: McpPromptInfo | null;
  onClose: () => void;
  onSubmit: (values: Record<string, string>) => void;
  result?: any;
};

const PromptModal = ({
  show,
  prompt,
  onClose,
  onSubmit,
  result,
}: PromptModalProps) => {
  const { Modal, Input, Button, Badge } = useTheme();
  const [formValues, setFormValues] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [view, setView] = useState<"form" | "result">("form");

  useEffect(() => {
    if (prompt?.arguments) {
      // Reset form values when prompt changes
      const initial: Record<string, string> = {};
      prompt.arguments.forEach((a: McpPromptArgument) => {
        initial[a.name] = "";
      });
      setFormValues(initial);
      setTouched({});
    }
    setView("form");
  }, [prompt, show]);

  // Switch to result view when result is set
  useEffect(() => {
    if (result !== undefined && result !== null) {
      setView("result");
    }
  }, [result]);

  if (!prompt) return null;

  const handleChange = (name: string, value: string) => {
    setFormValues((v) => ({ ...v, [name]: value }));
    setTouched((t) => ({ ...t, [name]: true }));
  };

  const requiredArgs = (prompt.arguments || []).filter((a) => a.required);
  const isValid =
    requiredArgs.length === 0 ||
    requiredArgs.every((a) => !!formValues[a.name]?.trim());

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (isValid) {
      onSubmit(formValues);
    }
  };

  const allExpanded = () => true;

  return (
    <Modal show={show} onHide={onClose} size="lg" title={prompt.name}>
      <div>
        <div style={{ marginBottom: 12, color: "#666" }}>
          {prompt.description}
        </div>
        {view === "form" ? (
          <form onSubmit={handleSubmit}>
            {prompt.arguments && prompt.arguments.length > 0 ? (
              <div
                style={{ display: "flex", flexDirection: "column", gap: 16 }}
              >
                {prompt.arguments.map((arg) => (
                  <div key={arg.name}>
                    <label style={{ fontWeight: 500 }}>
                      {arg.name}
                      {arg.required && (
                        <span style={{ marginLeft: 6 }}>
                          <Badge bg="danger">required</Badge>
                        </span>
                      )}
                    </label>
                    <div
                      style={{ color: "#666", fontSize: 13, marginBottom: 2 }}
                    >
                      {arg.description}
                    </div>
                    <Input
                      type="text"
                      required={arg.required}
                      value={formValues[arg.name] || ""}
                      onChange={(e: any) =>
                        handleChange(
                          arg.name,
                          e?.target?.value !== undefined ? e.target.value : e
                        )
                      }
                      placeholder={arg.description || arg.name}
                      style={{
                        borderColor:
                          touched[arg.name] &&
                          arg.required &&
                          !formValues[arg.name]
                            ? "#dc3545"
                            : undefined,
                      }}
                    />
                  </div>
                ))}
              </div>
            ) : (
              <div style={{ color: "#888", marginBottom: 16 }}>
                No arguments required for this prompt.
              </div>
            )}
            <div style={{ marginTop: 24, display: "flex", gap: 8 }}>
              <Button variant="primary" type="submit" disabled={!isValid}>
                Get prompt
              </Button>
              <Button variant="secondary" onClick={onClose} type="button">
                Close
              </Button>
            </div>
          </form>
        ) : (
          <div>
            <div style={{ marginTop: 8 }}>
              <JsonView
                data={result}
                shouldExpandNode={allExpanded}
                style={defaultStyles}
              />
            </div>
            <div style={{ marginTop: 24, display: "flex", gap: 8 }}>
              <Button variant="secondary" onClick={() => setView("form")}>
                Back
              </Button>
              <Button variant="primary" onClick={onClose}>
                Close
              </Button>
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};

export default PromptModal;
