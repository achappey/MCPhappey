import { useState } from "react";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { ContentBlockView } from "./tools/ContentBlockView";
import { ToolCallResult } from "mcphappeditor-types";
import { StructuredOutputView } from "./tools/StructuredOutputView";

export interface ShowToolCallResultProps {
  open: boolean;
  onClose: () => void;
  result: ToolCallResult;
}

export const ShowToolCallResult = ({
  open,
  onClose,
  result,
}: ShowToolCallResultProps) => {
  const { Modal, Button, Tabs, Tab } = useTheme();
  const { t } = useTranslation();
  //const jsonString =
  //typeof result === "string" ? result : JSON.stringify(result, null, 2);

  if (!open) return null;

  // Try to get content array
  const contentArr = Array.isArray(result?.content) ? result.content : [];
  const [activeTab, setActiveTab] = useState("0");

  return (
    <Modal
      show={open}
      onHide={onClose}
      actions={<Button onClick={onClose}>{t("close")}</Button>}
      title={t("mcp.toolCallResult")}
    >
      <div>
        {result.structuredContent ? (
          <StructuredOutputView result={result} />
        ) : null}
        {!result.structuredContent && contentArr.length > 0 ? (
          <Tabs activeKey={activeTab} onSelect={(k: string) => setActiveTab(k)}>
            {contentArr.map((block: any, i: number) => (
              <Tab
                key={String(i)}
                eventKey={String(i)}
                title={t(`mcp.${block.type}`)}
              >
                <div style={{ padding: 8 }}>
                  <ContentBlockView block={block} />
                </div>
              </Tab>
            ))}
          </Tabs>
        ) : null}
      </div>
    </Modal>
  );
};
