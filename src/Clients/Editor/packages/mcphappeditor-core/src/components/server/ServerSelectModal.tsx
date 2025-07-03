import { useState, useMemo } from "react";
import { useAppStore } from "mcphappeditor-state";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { CancelButton } from "../common/CancelButton";
import { AddServerModal } from "./AddServerModal";

type Props = {
  show: boolean;
  onHide: () => void;
};

export const ServerSelectModal = ({ show, onHide }: Props) => {
  const { Modal, Button, Switch, SearchBox } = useTheme();
  const { t } = useTranslation();
  const selected = useAppStore((s) => s.selected);
  const selectServers = useAppStore((s) => s.selectServers);
  const servers = useAppStore((s) => s.servers);

  const [checked, setChecked] = useState<Set<string>>(new Set(selected));
  const [showManage, setShowManage] = useState(false);
  const [search, setSearch] = useState("");

  const handleToggle = (name: string) => {
    setChecked((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });
  };

  const handleOk = () => {
    selectServers(Array.from(checked));
    onHide();
  };

  const handleCancel = () => {
    setChecked(new Set(selected));
    onHide();
  };

  // Memoized, filtered server entries based on search
  const filteredServers = useMemo(
    () =>
      Object.entries(servers).filter(([name]) =>
        name.toLowerCase().includes(search.trim().toLowerCase())
      ),
    [servers, search]
  );

  // Count enabled (checked) servers
  const enabledCount = checked.size;

  return (
    <Modal
      show={show}
      onHide={handleCancel}
      actions={
        <>
          <Button
            onClick={() => setShowManage(true)}
            variant="secondary"
            type="button"
          >
            {t("serverSelectModal.manage")}
          </Button>
          <CancelButton onClick={handleOk} />
          <Button onClick={handleOk} type="button">
            {t("ok")}
          </Button>
        </>
      }
      title={t("serverSelectModal.title", { enabled: enabledCount })}
    >
      <SearchBox
        value={search}
        onChange={setSearch}
        placeholder={t("searchPlaceholder")}
        autoFocus
      />
      <div
        style={{
          minWidth: 320,
          maxHeight: 400,
          overflowY: "auto",
          marginBottom: 16,
          position: "relative",
        }}
      >
        {/* Server switches */}
        <div style={{ marginTop: 8 }}>
          {filteredServers.length === 0 && (
            <div style={{ color: "#888" }}>
              {t("serverSelectModal.noServers")}
            </div>
          )}
          {filteredServers.map(([name, cfg]) => (
            <div
              key={name}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                marginBottom: 8,
              }}
            >
              <Switch
                id={`switch-${name}`}
                label={name}
                checked={checked.has(name)}
                onChange={() => handleToggle(name)}
              />
            </div>
          ))}
        </div>
      </div>
      <AddServerModal show={showManage} onHide={() => setShowManage(false)} />
    </Modal>
  );
};
