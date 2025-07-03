import { useTheme } from "../../ThemeContext";
import type { Prompt } from "mcphappeditor-mcp";

type PromptCardProps = {
  prompt: Prompt & { _url?: string; text?: string };
  onSelect?: () => void;
};

export const PromptCard = ({ prompt, onSelect }: PromptCardProps) => {
  const { Card, Button } = useTheme();

  return (
    <Card
      title={prompt.name}
      description={prompt.description || prompt.text}
      size="small"
      actions={
        <Button
          onClick={onSelect}
          variant="transparent"
          icon="add"
          size="small"
        />
      }
    />
  );
};