import { MenuItemProps } from "mcphappeditor-types";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { useCallback } from "react";

type Props = {
  name: string;
  url: string;
  type: "http" | "sse";
  onRemove: () => void;
};

export const ServerCard = ({ name, url, type, onRemove }: Props) => {
  const { Card, Menu, Button } = useTheme();
  const { t } = useTranslation();

  const copyToClipboard = useCallback(async () => {
    const blob = new Blob([url], { type: "text/html" });
    const textBlob = new Blob([url], { type: "text/plain" });
    const data = [
      new ClipboardItem({ "text/html": blob, "text/plain": textBlob }),
    ];
    await navigator.clipboard.write(data);
  }, [url]);

  // Menu actions (only delete here, but can be extended)
  const actions: MenuItemProps[] = [
    {
      key: "delete",
      label: t("delete"),
      onClick: onRemove,
    },
  ];

  return (
    <Card
      title={name}
      text={url}
      size="small"
      actions={
        <>
          <Button
            onClick={copyToClipboard}
            variant="transparent"
            icon="copyClipboard"
            size="small"
          />
        </>
      }
      headerActions={<Menu items={actions} />}
    >
      <div style={{ fontSize: 12, color: "#888" }}>{url}</div>
    </Card>
  );
};
