import React, { useState, useEffect, useMemo } from "react";
import type { McpToolInfo } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import { JsonView, defaultStyles } from "react-json-view-lite";
import "react-json-view-lite/dist/index.css";

type ToolModalProps = {
  show: boolean;
  tool: McpToolInfo | null;
  onClose: () => void;
  onSubmit: (values: Record<string, any>) => void;
  result?: any;
};

type SchemaProperty = {
  description?: string;
  type?: string | string[];
  enum?: (string | number | boolean | null)[];
  default?: any;
};

const ToolModal = ({
  show,
  tool,
  onClose,
  onSubmit,
  result,
}: ToolModalProps) => {
  const { Modal, Input, Button, Badge } = useTheme();
  const [formValues, setFormValues] = useState<Record<string, any>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [view, setView] = useState<"form" | "result">("form");

  const schema = tool?.inputSchema ?? {};
  const properties: Record<string, SchemaProperty> =
    schema.properties ?? {};

  const requiredSet = useMemo(() => {
    const req = schema.required as string[] | undefined;
    return new Set(req ?? []);
  }, [schema.required]);

  // Reset when tool changes
  useEffect(() => {
    if (tool) {
      const initial: Record<string, any> = {};
      Object.entries(properties).forEach(([name, prop]) => {
        if (prop.default !== undefined) initial[name] = prop.default;
        else if (prop.type === "boolean") initial[name] = false;
        else initial[name] = "";
      });
      setFormValues(initial);
      setTouched({});
    }
    setView("form");
  }, [tool, show]); // eslint-disable-line react-hooks/exhaustive-deps

  // Switch to result view when result arrives
  useEffect(() => {
    if (result !== undefined && result !== null) setView("result");
  }, [result]);

  if (!tool) return null;

  const handleChange = (name: string, value: any) => {
    setFormValues((v) => ({ ...v, [name]: value }));
    setTouched((t) => ({ ...t, [name]: true }));
  };

  const isValid = () => {
    for (const key of requiredSet) {
      const v = formValues[key];
      if (
        v === undefined ||
        v === null ||
        (typeof v === "string" && !v.trim())
      )
        return false;
    }
    return true;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (isValid()) {
      // Convert empty strings to null for enum properties that include null
      const fixed: Record<string, any> = {};
      Object.entries(properties).forEach(([name, prop]) => {
        let val = formValues[name];
        if (val === "" && prop.enum?.includes(null)) val = null;
        fixed[name] = val;
      });
      onSubmit(fixed);
    }
  };

  const allExpanded = () => true;

  const renderField = (name: string, prop: SchemaProperty) => {
    const required = requiredSet.has(name);
    const typeArray = Array.isArray(prop.type)
      ? prop.type
      : prop.type
      ? [prop.type]
      : [];

    if (prop.enum && prop.enum.length > 0) {
      return (
        <select
          value={formValues[name] ?? ""}
          onChange={(e) => handleChange(name, e.target.value)}
          required={required}
          style={{ padding: 6, width: "100%" }}
        >
          {!required && !prop.enum.includes(null) && (
            <option value="">-- none --</option>
          )}
          {prop.enum.map((opt) => (
            <option key={String(opt)} value={opt === null ? "" : String(opt)}>
              {opt === null ? "-- none --" : String(opt)}
            </option>
          ))}
        </select>
      );
    }

    if (typeArray.includes("boolean")) {
      return (
        <input
          type="checkbox"
          checked={!!formValues[name]}
          onChange={(e) => handleChange(name, e.target.checked)}
        />
      );
    }

    const inputType = typeArray.includes("number") ? "number" : "text";
    return (
      <Input
        type={inputType}
        required={required}
        value={formValues[name] ?? ""}
        onChange={(e: any) =>
          handleChange(
            name,
            e?.target?.value !== undefined ? e.target.value : e
          )
        }
        placeholder={prop.description || name}
        style={{
          borderColor:
            touched[name] && required && !formValues[name] ? "#dc3545" : undefined,
        }}
      />
    );
  };

  return (
    <Modal show={show} onHide={onClose} size="lg" title={tool.name}>
      <div>
        <div style={{ marginBottom: 12, color: "#666" }}>
          {tool.description}
        </div>
        {view === "form" ? (
          <form onSubmit={handleSubmit}>
            {Object.keys(properties).length > 0 ? (
              <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                {Object.entries(properties).map(([name, prop]) => (
                  <div key={name}>
                    <label style={{ fontWeight: 500 }}>
                      {name}
                      {requiredSet.has(name) && (
                        <span style={{ marginLeft: 6 }}>
                          <Badge bg="danger">required</Badge>
                        </span>
                      )}
                    </label>
                    <div
                      style={{ color: "#666", fontSize: 13, marginBottom: 2 }}
                    >
                      {prop.description}
                    </div>
                    {renderField(name, prop)}
                  </div>
                ))}
              </div>
            ) : (
              <div style={{ color: "#888", marginBottom: 16 }}>
                No inputs required for this tool.
              </div>
            )}
            <div style={{ marginTop: 24, display: "flex", gap: 8 }}>
              <Button variant="primary" type="submit" disabled={!isValid()}>
                Call tool
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

export default ToolModal;
