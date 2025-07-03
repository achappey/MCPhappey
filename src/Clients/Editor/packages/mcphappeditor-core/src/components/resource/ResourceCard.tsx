import { Resource } from "mcphappeditor-mcp";
import { useTheme } from "../../ThemeContext";

type ResourceCardProps = {
  resource: Resource;
  onSelect?: () => void;
};

export const ResourceCard = ({ resource, onSelect }: ResourceCardProps) => {
  const { Card, Button } = useTheme();

  return (
    <Card
      title={resource.name}
      description={resource.description || resource.uri}
      size="small"
      actions={
        <Button
          onClick={onSelect}
          variant="transparent"
          icon="add"
          size="small"
        />
      }
    ></Card>
  );
};
