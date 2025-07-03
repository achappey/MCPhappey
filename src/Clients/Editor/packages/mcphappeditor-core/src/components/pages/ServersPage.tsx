import { useState } from "react";
import { useAppStore } from "mcphappeditor-state";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { ServerCard } from "../server/ServerCard";
import { ServersHeader } from "../server/ServersHeader";
import { AddServerModal } from "../server/AddServerModal";
import { ProtocolPageHeader } from "../common/ProtocolPageHeader";

export const ServersPage = () => {
  const { SearchBox, Paragraph } = useTheme();
  const { t } = useTranslation();
  const servers = useAppStore((s) => s.servers);
  const removeServer = useAppStore((s) => s.removeServer);
  const [showModal, setShowModal] = useState(false);
  const [search, setSearch] = useState("");

  const filteredServers = Object.entries(servers)
  .filter(([name]) =>
    name.toLowerCase().includes(search.trim().toLowerCase())
  )
  .sort(([a], [b]) => a.localeCompare(b));
  
  return (
    <>
      <ServersHeader onAddServer={() => setShowModal(true)} />
      <div
        style={{
          background: "transparent",
        }}
      >
        <div
          style={{
            width: 700,
            maxWidth: "100%",
            margin: "0 auto",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
          }}
        >
          <ProtocolPageHeader
            title={t("mcpPage.title")}
            officialUrl={"https://modelcontextprotocol.io/"}
            docsUrl={"https://github.com/modelcontextprotocol"}
          />

          <Paragraph style={{ textAlign: "center" }}>
            {t("mcpPage.description")}
          </Paragraph>

          <div
            style={{
              width: "100%",
              display: "flex",
              justifyContent: "center",
              marginBottom: 16,
            }}
          >
            <div style={{ width: 360, maxWidth: "100%" }}>
              <SearchBox
                value={search}
                onChange={setSearch}
                placeholder={t("searchPlaceholder")}
              />
            </div>
          </div>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: 16,
              width: "100%",
              maxWidth: 700,
              marginBottom: 24,
              justifyItems: "center",
            }}
          >
            {filteredServers.length === 0 ? (
              <div
                style={{
                  color: "#888",
                  gridColumn: "1 / -1",
                  textAlign: "center",
                }}
              >
                {t("serverSelectModal.noServers")}
              </div>
            ) : (
              filteredServers.map(([name, cfg]) => (
                <div key={name} style={{ maxWidth: 320, width: "100%" }}>
                  <ServerCard
                    name={name}
                    url={cfg.url}
                    type={cfg.type}
                    onRemove={() => removeServer(name)}
                  />
                </div>
              ))
            )}
          </div>
          <AddServerModal show={showModal} onHide={() => setShowModal(false)} />
        </div>
      </div>
    </>
  );
};
