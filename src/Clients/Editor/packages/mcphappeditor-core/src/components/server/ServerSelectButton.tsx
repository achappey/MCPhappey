import { useState } from "react";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { ServerSelectModal } from "./ServerSelectModal";

export const ServerSelectButton = () => {
  const { Button } = useTheme();
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        type="button"
        icon="mcpServer"
        variant="transparent"
        onClick={() => setOpen(true)}
        title={t("serverSelectModal.title")}
      ></Button>
      <ServerSelectModal show={open} onHide={() => setOpen(false)} />
    </>
  );
};
