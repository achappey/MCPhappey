import React, { useState } from "react";
import { useTheme } from "../../ThemeContext";
import { useAppStore } from "mcphappeditor-state";
import { useTranslation } from "mcphappeditor-i18n";

export interface SettingsModalProps {
  open: boolean;
  onClose: () => void;
}

export const SettingsModal: React.FC<SettingsModalProps> = ({
  open,
  onClose,
}) => {
  const theme = useTheme();
  const { Modal, Button, Select, Switch } = theme;
  const { t, i18n } = useTranslation(); // Uncomment when i18n is ready
  const [activeTab, setActiveTab] = useState("general");
  const remoteStorageConnected = useAppStore((s) => s.remoteStorageConnected);
  const setLogLevel = useAppStore((s) => s.setLogLevel);
  const logLevel = useAppStore((s) => s.logLevel);
  const setRemoteStorageConnected = useAppStore(
    (s) => s.setRemoteStorageConnected
  );

  // Hardcoded language options for now
  const languageOptions = [
    { value: "en", label: t("locales.enUS") },
    { value: "nl", label: t("locales.nlNL") },
  ];

  const logLevelOptions = [
    { value: "debug", label: t("logLevels.debug") },
    { value: "info", label: t("logLevels.info") },
    { value: "notice", label: t("logLevels.notice") },
    { value: "warning", label: t("logLevels.warning") },
    { value: "error", label: t("logLevels.error") },
    { value: "critical", label: t("logLevels.critical") },
    { value: "alert", label: t("logLevels.alert") },
    { value: "emergency", label: t("logLevels.emergency") },
  ];

  return (
    <Modal show={open} onHide={onClose} title={t("settingsModal.title")}>
      <div
        style={{
          borderRadius: 12,
          padding: 0,
          overflow: "hidden",
          display: "flex",
          flexDirection: "column",
          height: "100%",
        }}
      >
        <div
          style={{
            flex: 1,
            display: "flex",
            flexDirection: "column",
            padding: "24px 0 0 0",
          }}
        >
          <theme.Tabs
            activeKey={activeTab}
            vertical={true}
            style={{ minHeight: 260 }}
            onSelect={setActiveTab}
          >
            <theme.Tab
              eventKey="general"
              icon={"settings"}
              title={t("settingsModal.tabGeneral")}
            >
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: 32,
                  padding: "0 32px 32px 32px",
                }}
              >
                {/* Language */}
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div style={{ fontSize: 16 }}>
                    {t("settingsModal.languageLabel")}
                  </div>
                  <Select
                    value={i18n.language}
                    valueTitle={
                      languageOptions.find((a) => a.value == i18n.language)
                        ?.label
                    }
                    options={languageOptions}
                    onChange={(v: string) => i18n.changeLanguage(v)}
                  >
                    {languageOptions.map((language) => (
                      <option key={language.value} value={language.value}>
                        {language.label}
                      </option>
                    ))}
                  </Select>
                </div>
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div style={{ fontSize: 16 }}>
                    {t("settingsModal.logLevel")}
                  </div>
                  <Select
                    value={logLevel}
                    valueTitle={t(`logLevels.${logLevel}`)}
                    options={logLevelOptions}
                    onChange={async (v: any) => await setLogLevel(v)}
                  >
                    {logLevelOptions.map((logLevel) => (
                      <option key={logLevel.value} value={logLevel.value}>
                        {logLevel.label}
                      </option>
                    ))}
                  </Select>
                </div>
                {/* Delete all chats */}
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div style={{ fontSize: 16 }}>
                    {t("settingsModal.deleteAllChats")}
                  </div>
                  <Button
                    className="danger"
                    variant="subtle"
                    onClick={() => {
                      /* TODO: Implement delete all chats */
                    }}
                  >
                    {t("settingsModal.deleteAll")}
                  </Button>
                </div>
                {/* Log out */}
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div style={{ fontSize: 16 }}>
                    {t("settingsModal.logoutOnDevice")}
                  </div>
                  <Button
                    className="primary"
                    onClick={() => {
                      /* TODO: Implement logout */
                    }}
                  >
                    {t("userMenu.logout")}
                  </Button>
                </div>
              </div>
            </theme.Tab>
            <theme.Tab
              eventKey="personalization"
              icon={"personalization"}
              title={t("settingsModal.tabPersonalization")}
            >
              <div style={{ color: "#888", fontSize: 16, padding: "32px 0" }}>
                {t("settingsModal.personalizationComingSoon")}
              </div>
            </theme.Tab>
            <theme.Tab
              eventKey="data"
              icon={"databaseGear"}
              title={t("settingsModal.tabData")}
            >
              <div style={{ color: "#888", fontSize: 16, padding: "32px 0" }}>
                {t("settingsModal.dataControlsComingSoon")}
              </div>
            </theme.Tab>
            <theme.Tab
              eventKey="connectors"
              icon={"connector"}
              title={t("settingsModal.tabConnectors")}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  padding: "32px 32px",
                }}
              >
                <div style={{ fontSize: 16 }}>
                  {t("settingsModal.remoteStorage")}
                </div>
                <Switch
                  id="remote-storage-toggle"
                  checked={remoteStorageConnected}
                  label={t("settingsModal.remoteStorage")}
                  onChange={() =>
                    setRemoteStorageConnected(!remoteStorageConnected)
                  }
                />
              </div>
            </theme.Tab>
          </theme.Tabs>
        </div>
      </div>
    </Modal>
  );
};

export default SettingsModal;
