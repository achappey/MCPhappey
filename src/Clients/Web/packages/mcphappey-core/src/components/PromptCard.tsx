import type { McpPromptInfo } from "mcphappey-types";
import { useTheme } from "../ThemeContext";

type PromptCardProps = {
  prompt: McpPromptInfo;
  onOpen: (prompt: McpPromptInfo) => void;
};

const PromptCard = ({ prompt, onOpen }: PromptCardProps) => {
  const { Card, Button } = useTheme();
  return (
    <Card
      title={prompt.name}
      text={prompt.description ?? ""}
      actions={
        <Button
          variant="primary"
          size="sm"
          onClick={() => onOpen(prompt)}
        >
          Open
        </Button>
      }
    />
  );
};

export default PromptCard;
