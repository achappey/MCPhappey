import type { McpResourceInfo } from "mcphappey-types";
import { useTheme } from "../ThemeContext";

type ResourceCardProps = {
  resource: McpResourceInfo;
  onRead: (resource: McpResourceInfo) => void;
  onOpenLink: (resource: McpResourceInfo) => void;
};

const ResourceCard = ({ resource, onRead, onOpenLink }: ResourceCardProps) => {
  const { Card, Button } = useTheme();

  return (
    <Card
      title={resource.name}
      text={resource.description ?? ""}
      actions={
        <div style={{ display: "flex", gap: 8 }}>
          <Button variant="primary" size="sm" onClick={() => onRead(resource)}>
            Read resource
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => onOpenLink(resource)}
          >
            Open link
          </Button>
        </div>
      }
    />
  );
};

export default ResourceCard;
