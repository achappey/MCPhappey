import { useTranslation } from "mcphappeditor-i18n";
import { useTheme } from "../../../ThemeContext";
import { ToolCallResult } from "mcphappeditor-types";
import { JsonViewer } from "../../common/JsonViewer";

interface StructuredOutputViewProps {
  result: ToolCallResult;
}

export const StructuredOutputView = ({ result }: StructuredOutputViewProps) => {
  const { Card } = useTheme();
  const { t } = useTranslation();

  return (
    <Card
      title={t("mcp.structuredContent")}
      size="small"
      children={<JsonViewer value={result.structuredContent} />}
    />
  );
};
