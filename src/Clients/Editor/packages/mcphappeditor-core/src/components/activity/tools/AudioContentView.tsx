import { useTheme } from "../../../ThemeContext";

interface AudioContentViewProps {
  block: { type: "audio"; data: string; mimeType: string };
}

export const AudioContentView = ({ block }: AudioContentViewProps) => {
  const { Card } = useTheme();
  const src = `data:${block.mimeType};base64,${block.data}`;
  return (
    <Card title="Audio" size="small">
      <audio controls style={{ width: "100%" }}>
        <source src={src} type={block.mimeType} />
        Your browser does not support the audio element.
      </audio>
    </Card>
  );
};