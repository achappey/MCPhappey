import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { ToolInvocationCard } from "../activity/tools/ToolInvocationCard";

interface ToolDrawerProps {
  open: boolean;
  tools: any[];
  onClose: () => void;
}

export const ToolDrawer = ({ open, tools, onClose }: ToolDrawerProps) => {
  const { Drawer } = useTheme();
  const { t } = useTranslation();

  return (
    <Drawer open={open} overlay title={t("mcp.tools")} onClose={onClose}>
      <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
        {tools.map((tool, i) => (
          <ToolInvocationCard
            invocation={{
              type: tool.type,
              input: tool.input,
              role: "assistant",
              state: tool.state,
              output: tool.output,
              toolCallId: tool.toolCallId,
            }}
          />
        ))}
      </div>
    </Drawer>
  );
};
