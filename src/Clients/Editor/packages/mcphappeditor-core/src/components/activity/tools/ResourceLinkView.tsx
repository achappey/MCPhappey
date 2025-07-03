import { useTheme } from "../../../ThemeContext";

interface ResourceLinkViewProps {
  block: { type: "resource_link"; uri: string };
}

export const ResourceLinkView = ({ block }: ResourceLinkViewProps) => {
  const { Card, Button } = useTheme();
  return (
    <Card
      title="Resource Link"
      size="small"
      text={block.uri}
      actions={
        <Button
          size="small"
          onClick={() => window.open(block.uri, "_blank")}
        >
          Open
        </Button>
      }
    />
  );
};