import { useState } from "react";
import type { ElicitResult, ElicitRequest } from "mcphappeditor-mcp";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";

type Props = {
  id: string;
  params: ElicitRequest["params"];
  onRespond: (r: ElicitResult) => void;
};

export const ElicitForm = ({ params, onRespond }: Props) => {
  const { message, requestedSchema } = params;
  const { t } = useTranslation();
  const { Card, Button, Input, Select, Switch, Paragraph } = useTheme();
  const [values, setValues] = useState<Record<string, any>>(() => {
    const v: Record<string, any> = {};
    Object.entries(requestedSchema.properties).forEach(
      ([k, s]: [string, any]) => {
        if (s.default !== undefined) v[k] = s.default;
        else if (s.type === "boolean") v[k] = false;
        else v[k] = "";
      }
    );
    return v;
  });

  const required = requestedSchema.required || [];
  const isValid = required.every(
    (k: string) => values[k] !== "" && values[k] !== undefined
  );

  const handleChange = (k: string, val: any) =>
    setValues((v) => ({ ...v, [k]: val }));

  const fields = Object.entries(requestedSchema.properties).map(
    ([k, s]: [string, any]) => {
      const label = s.title || k;
      if (s.type === "boolean") {
        return (
          <div key={k} style={{ marginBottom: 12 }}>
            <Switch
              id={k}
              label={label}
              hint={s.description}
              checked={!!values[k]}
              onChange={(val) => handleChange(k, val)}
            />
          </div>
        );
      }
      if (s.enum) {
        // <div style={{ fontWeight: 600, marginBottom: 4 }}>{label}</div>
        return (
          <div key={k} style={{ marginBottom: 12 }}>
            <Select
              value={values[k]}
              hint={s.description}
              label={label}
              placeholder={`${t("select")}...`}
              onChange={(val: boolean) => handleChange(k, val)}
              aria-label={label}
            >
              {s.enum.map((opt: string, i: number) => (
                <option key={i} value={opt}>
                  {s.enumNames?.[i] || opt}
                </option>
              ))}
            </Select>
          </div>
        );
      }

      const type =
        s.type === "number" || s.type === "integer"
          ? "number"
          : s.format === "email"
          ? "email"
          : s.format === "uri"
          ? "url"
          : s.format === "date"
          ? "date"
          : s.format === "date-time"
          ? "datetime-local"
          : "text";

      return (
        <div key={k} style={{ marginBottom: 12 }}>
          <Input
            type={type}
            hint={s.description}
            value={values[k]}
            label={label}
            onChange={(e) =>
              handleChange(
                k,
                type === "number" ? Number(e.target.value) : e.target.value
              )
            }
            min={s.minimum}
            max={s.maximum}
            minLength={s.minLength}
            maxLength={s.maxLength}
            required={required.includes(k)}
          />
        </div>
      );
    }
  );

  const actions = (
    <>
      <Button
        type="button"
        variant="primary"
        disabled={!isValid}
        onClick={() => onRespond({ action: "accept", content: values })}
      >
        {t("mcp.accept")}
      </Button>
      <Button
        type="button"
        variant="outline-danger"
        onClick={() => onRespond({ action: "reject" })}
      >
        {t("mcp.reject")}
      </Button>
      <Button
        type="button"
        variant="outline-secondary"
        onClick={() => onRespond({ action: "cancel" })}
      >
        {t("mcp.cancel")}
      </Button>
    </>
  );

  return (
    <Card
      title={message}
      style={{ backgroundColor: "transparent" }}
      actions={actions}
    >
      {fields}
    </Card>
  );
};
