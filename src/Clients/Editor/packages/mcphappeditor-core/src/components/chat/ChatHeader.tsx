import { useEffect, useState } from "react";
import { ModelSelect } from "../models/ModelSelect";
import { useChatContext } from "../chat/ChatContext";
import { useModels } from "../models/useModels";
import { useAppStore } from "mcphappeditor-state";
import { useTheme } from "../../ThemeContext";
import { UserMenuButton } from "../common/UserMenuButton";
import { useAccount } from "mcphappeditor-auth";
import SettingsModal from "../settings/SettingsModal";
import { useTranslation } from "mcphappeditor-i18n";
import { useLocation } from "react-router";
import { useDarkMode } from "usehooks-ts";

interface ChatHeaderProps {
  value?: string;
  onChange: (id: string) => void;
}

export const ChatHeader: React.FC<ChatHeaderProps> = ({ value, onChange }) => {
  const { config } = useChatContext();
  const [settingsOpen, setSettingsOpen] = useState(false);

  const { models, loading } = useModels(
    config.modelsApi!,
    config.getAccessToken
  );
  const { isDarkMode } = useDarkMode();
  const showActivities = useAppStore((a) => a.showActivities);
  const toggleActivities = useAppStore((a) => a.toggleActivities);
  const { Switch } = useTheme();
  const { t } = useTranslation();
  const account = useAccount();
  const { pathname } = useLocation();
  const email = account?.username;

  useEffect(() => {
    if (!loading && models.length && !value) {
      onChange(models[0].id);
    }
  }, [loading, models, value, onChange]);

  return (
    <>
      <div
        style={{
          position: "sticky",
          top: 0,
          zIndex: 10,
          backgroundColor: isDarkMode ? "#292929" : "#ffffff",
          height: 48,
          display: "flex",
          alignItems: "center",
          padding: "0 12px",
          //   gap: 16,
        }}
      >
        <ModelSelect
          models={models}
          value={value ?? ""}
          onChange={onChange}
          disabled={loading}
        />
        <div style={{ flex: 1 }} />
        {pathname != "" && pathname != "/" ? (
          <Switch
            onChange={toggleActivities}
            id="activities-toggle"
            checked={showActivities}
          />
        ) : null}
        <div style={{ paddingLeft: 16 }}>
          <UserMenuButton
            email={email}
            onCustomize={() => console.log("Customize clicked")}
            onSettings={() => setSettingsOpen(true)}
            onLogout={() => console.log("Logout clicked")}
          />
        </div>
      </div>
      <SettingsModal
        open={settingsOpen}
        onClose={() => setSettingsOpen(false)}
      />
    </>
  );
};
