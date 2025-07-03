import { useTranslation } from "mcphappeditor-i18n";
import { useTheme } from "../../../ThemeContext";
import { EmbeddedResourceContent } from "./EmbeddedResourceContent";

interface EmbeddedResourceViewProps {
  block: {
    type: "resource";
    resource: { uri: string; mimeType: string; text?: string; data?: string };
  };
}

export const EmbeddedResourceView = ({ block }: EmbeddedResourceViewProps) => {
  const { Card, Button } = useTheme();
  const { t } = useTranslation();
  const uri = block.resource?.uri;

  let buttonLabel = t("open");
  if (uri) {
    try {
      buttonLabel = new URL(uri).hostname.replace(/^www\./, "");
    } catch {}
  }

  return (
    <Card
      title={block.resource?.text ? t("mcp.embeddedTextResource") : t("mcp.embeddedBlobResource")}
      size="small"
      children={<EmbeddedResourceContent resource={block.resource} />}
      actions={
        uri ? (
          <Button
            size="small"
            icon="link"
            onClick={() => window.open(uri, "_blank")}
          >
            {buttonLabel}
          </Button>
        ) : undefined
      }
    />
  );
};
