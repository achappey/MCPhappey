// Simple disclaimer bar for chat, sticky at bottom, i18n text, appName and version from context
import { useChatContext } from "./ChatContext";
import { useTranslation } from "mcphappeditor-i18n";
import { useMediaQuery } from "usehooks-ts";

const DisclaimerBar = () => {
  const { config } = useChatContext();
  const { t } = useTranslation();
  const appName = config.appName ?? "AIHappey";
  const appVersion = config.appVersion ?? "";
  const isDesktop = useMediaQuery("(min-width: 768px)");

  return (
    <div
      style={{
        width: "100%",
        position: "sticky",
        bottom: 0,
        left: 0,
        fontSize: 10,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 2,
        padding: "0 16px",
        boxSizing: "border-box",
      }}
      data-testid="disclaimer-bar"
    >
      <span style={{ flex: 1, textAlign: "center" }}>
        {t("disclaimer", { appName })}
      </span>
      {isDesktop && appVersion && (
        <span
          style={{
            marginLeft: "auto",
            paddingLeft: 16,
          }}
        >
          {appVersion}
        </span>
      )}
    </div>
  );
};

export { DisclaimerBar };
