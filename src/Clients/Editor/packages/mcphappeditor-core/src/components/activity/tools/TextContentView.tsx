import { useTheme } from "../../../ThemeContext";
import { Markdown } from "../../markdown/Markdown";

interface TextContentViewProps {
  block: { type: "text"; text: string };
}

export const TextContentView = ({ block }: TextContentViewProps) => {
  const { Card } = useTheme();
  return (
    <Card
      title="Text"
      size="small"
      children={<Markdown text={block.text} />}
    />
  );
};
