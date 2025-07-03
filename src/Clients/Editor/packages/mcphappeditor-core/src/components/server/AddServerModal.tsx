import { useState, useMemo } from "react";
import { useAppStore } from "mcphappeditor-state";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { connectionTest } from "mcphappeditor-mcp";

type Props = {
  show: boolean;
  onHide: () => void;
};

export const AddServerModal = ({ show, onHide }: Props) => {
  const { Modal, Button, Input, TextArea } = useTheme();
  const { t } = useTranslation();
  const addServer = useAppStore((s) => s.addServer);
  const servers = useAppStore((s) => s.servers);
  const [name, setName] = useState("");
  const [url, setUrl] = useState("");
  const [srvType, setSrvType] = useState<"http" | "sse">("http");
  const [headers, setHeaders] = useState("");
  const [validated, setValidated] = useState(false);

  const nameExists = !!servers[name];

  // Memoised header validation (avoids state updates during render)
  const { error: headersError, parsedHeaders } = useMemo(() => {
    if (headers.trim() === "") {
      return { error: null, parsedHeaders: undefined };
    }
    try {
      const parsed = JSON.parse(headers);
      if (
        typeof parsed === "object" &&
        parsed !== null &&
        !Array.isArray(parsed) &&
        Object.values(parsed).every((v) => typeof v === "string")
      ) {
        return {
          error: null,
          parsedHeaders: parsed as Record<string, string>,
        };
      }
      return {
        error: 'Invalid JSON object: must be { "Header": "Value" }',
        parsedHeaders: undefined,
      };
    } catch {
      return {
        error: "Invalid JSON: must be a valid object",
        parsedHeaders: undefined,
      };
    }
  }, [headers]);

  const headersValid = !headersError;

  const canAdd =
    name.trim() !== "" &&
    url.trim() !== "" &&
    !nameExists &&
    headersValid &&
    validated;

  const handleAdd = () => {
    if (!canAdd) return;
    const cfg: any = { type: srvType, url };
    if (parsedHeaders) {
      cfg.headers = parsedHeaders;
    }
    addServer(name, cfg);
    setName("");
    setUrl("");
    setSrvType("http");
    setHeaders("");
    onHide();
  };

  const hide = () => {
    setName("");
    setUrl("");
    setSrvType("http");
    setHeaders("");
    onHide();
  };

  const validate = async () => {
    const status = await connectionTest(url, srvType);

    setValidated(status != "error");
  };

  return (
    <Modal
      show={show}
      onHide={onHide}
      title={t("manageServersModal.add")}
      actions={
        <>
          <Button
            onClick={validate}
            variant={validated ? "subtle" : "primary"}
            type="button"
          >
            {t("manageServersModal.validate")}
          </Button>
          <Button onClick={hide} variant="subtle" type="button">
            {t("cancel")}
          </Button>
          <Button onClick={handleAdd} disabled={!canAdd} type="button">
            {t("ok")}
          </Button>
        </>
      }
    >
      <div>
        <Input
          placeholder={t("manageServersModal.name")}
          value={name}
          required
          label={t("manageServersModal.name")}
          onChange={(e: any) => setName(e.target.value)}
          style={{ flex: 1 }}
          autoFocus
        />
        <Input
          placeholder={t("manageServersModal.url")}
          value={url}
          required
          label={t("manageServersModal.url")}
          onChange={(e: any) => {
            setValidated(false);
            setUrl(e.target.value);
          }}
          style={{ flex: 2 }}
        />

        <div style={{ fontWeight: 600, margin: "8px 0" }}>
          {t("manageServersModal.type")}
        </div>
        <div style={{ display: "flex", gap: 8, marginBottom: 8 }}>
          <label style={{ display: "flex", alignItems: "center", gap: 4 }}>
            <Input
              type="radio"
              name="srv-type"
              value="http"
              checked={srvType === "http"}
              onChange={() => {
                setSrvType("http");
                setValidated(false);
              }}
            />
            {t("manageServersModal.http")}
          </label>
          <label style={{ display: "flex", alignItems: "center", gap: 4 }}>
            <Input
              type="radio"
              name="srv-type"
              value="sse"
              checked={srvType === "sse"}
              onChange={() => {
                setSrvType("sse");
                setValidated(false);
              }}
            />
            {t("manageServersModal.sse")}
          </label>
        </div>
        <div style={{ marginBottom: 8 }}>
          <TextArea
            rows={3}
            label={t("manageServersModal.headers")}
            placeholder='{ "X-My-Header": "value" }'
            value={headers}
            style={{ width: "100%" }}
            onChange={(v: any) => {
              setHeaders(typeof v === "string" ? v : v?.target?.value ?? "");
              setValidated(false);
            }}
          />
          {headersError && (
            <div style={{ color: "#c00", fontSize: 12, marginTop: 2 }}>
              {headersError}
            </div>
          )}
          {!headersError && headers.trim() !== "" && (
            <div style={{ color: "#888", fontSize: 12, marginTop: 2 }}>
              {t("manageServersModal.headersInfo") ||
                "JSON object of extra headers"}
            </div>
          )}
        </div>

        {nameExists && (
          <div style={{ color: "#c00", fontSize: 12, marginBottom: 8 }}>
            {t("manageServersModal.nameExists")}
          </div>
        )}
      </div>
    </Modal>
  );
};
