import type { McpToolInfo } from "mcphappey-types";
import { useTheme } from "../ThemeContext";

type ToolCardProps = {
  tool: McpToolInfo;
  onOpen: (tool: McpToolInfo) => void;
};

const ToolCard = ({ tool, onOpen }: ToolCardProps) => {
  const { Card, Button } = useTheme();
  return (
    <Card
      title={tool.name}
      text={tool.description ?? ""}
      actions={
        <Button
          variant="primary"
          size="sm"
          onClick={() => onOpen(tool)}
        >
          Open
        </Button>
      }
    />
  );
};

export default ToolCard;
