import React from "react";
import { useTheme } from "../ThemeContext";
import { JsonView, defaultStyles } from "react-json-view-lite";
import "react-json-view-lite/dist/index.css";

type ResourceModalProps = {
  show: boolean;
  onClose: () => void;
  resourceName: string;
  mimeType: string | undefined;
  content: any;
  loading: boolean;
  error: string | null;
};

const ResourceModal = ({
  show,
  onClose,
  resourceName,
  mimeType,
  content,
  loading,
  error,
}: ResourceModalProps) => {
  const { Modal, Spinner, Alert, Button } = useTheme();

  const renderContent = () => {
    if (loading) return <Spinner size="sm" />;
    if (error) return <Alert variant="danger">{error}</Alert>;
    if (content === null || content === undefined)
      return <span>No content</span>;

    // If the server already returned a JS object (or array), render it as JSON
    if (typeof content === "object") {
      return (
        <JsonView
          data={content}
          shouldExpandNode={() => true}
          style={defaultStyles}
        />
      );
    }

    if (mimeType?.startsWith("image/") && content instanceof Blob) {
      const url = URL.createObjectURL(content);
      return <img src={url} alt={resourceName} style={{ maxWidth: "100%" }} />;
    }

    // Try to parse JSON
    if (
      typeof content === "string" &&
      (mimeType?.includes("json") || mimeType?.includes("application/"))
    ) {
      try {
        const json = JSON.parse(content);
        return (
          <JsonView data={json} shouldExpandNode={() => true} style={defaultStyles} />
        );
      } catch {
        /* fall through */
      }
    }

    // Default: show as preformatted text
    return (
      <pre style={{ whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
        {typeof content === "string" ? content : String(content)}
      </pre>
    );
  };

  return (
    <Modal show={show} onHide={onClose} size="lg" title={resourceName}>
      <div style={{ marginBottom: 16, color: "#666" }}>mimeType: {mimeType}</div>
      {renderContent()}
      <div style={{ marginTop: 24, display: "flex", gap: 8 }}>
        <Button variant="secondary" onClick={onClose}>
          Close
        </Button>
      </div>
    </Modal>
  );
};

export default ResourceModal;
