import { useState, useRef, KeyboardEvent, FormEvent } from "react";
import { useTheme } from "../../ThemeContext";
import { PromptSelectButton } from "../prompt/PromptSelectButton";
import { ResourceSelectButton } from "../resource/ResourceSelectButton";
import { ServerSelectButton } from "../server/ServerSelectButton";
import { useAppStore } from "mcphappeditor-state";
import type { UiAttachment } from "mcphappeditor-state/src/slices/uiSlice";
import { TagItem } from "mcphappeditor-types";
import { useTranslation } from "mcphappeditor-i18n";

/**
 * MessageInput – refactored to a 2‑row layout.
 * ┌───────────────────────────────────────────────────────────────────────┐
 * │ TextArea (auto‑grow)                                                 │
 * └───────────────────────────────────────────────────────────────────────┘
 * ┌───────────────────────────────────────────────────────────────────────┐
 * │ Server ▸ Prompt ▸ Resource ▸ Attach ▸ Stop/Send                      │
 * └───────────────────────────────────────────────────────────────────────┘
 */
export const MessageInput = ({
  onSend,
  disabled = false,
  model,
  streaming = false,
  onStop,
}: {
  onSend: (content: string) => void;
  disabled?: boolean;
  model?: string;
  onStop?: () => void;
  streaming?: boolean;
}) => {
  const { Button, Tags, TextArea } = useTheme();

  // ────────────────────────────────────────────────  state  ─┐
  const [value, setValue] = useState("");
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const resourceResults = useAppStore((s) => s.resourceResults);
  const removeResourceResult = useAppStore((s) => s.removeResourceResult);
  const resources = useAppStore((s) => s.resources);
  const { t } = useTranslation();
  const attachments = useAppStore((s) => s.attachments as UiAttachment[]);
  const addAttachment = useAppStore((s) => s.addAttachment);
  const removeAttachment = useAppStore((s) => s.removeAttachment);
  // ────────────────────────────────────────────────────────────────────┘

  /* --------------------------------- handlers -------------------------------- */
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      Array.from(e.target.files).forEach((file) => addAttachment(file));
      e.target.value = ""; // reset so selecting same file twice still fires
    }
  };

  const handleChange = (val: string) => {
    setValue(val);
    // optional auto‑grow
    if (textareaRef.current) {
      textareaRef.current.style.height = "auto";
      const newHeight = Math.min(textareaRef.current.scrollHeight, 100);
      textareaRef.current.style.height = `${newHeight}px`;
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const canSend =
    !!value.trim() || attachments.length > 0 || resourceResults.length > 0;

  const handleSend = () => {
    const trimmed = value.trim();
    if (!canSend) return;

    if (streaming && onStop) {
      onStop();
    }

    onSend(trimmed);
    setValue("");
    if (textareaRef.current) textareaRef.current.style.height = "auto";
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    handleSend();
  };

  /* ------------------------------ helper mappers ----------------------------- */
  const getResourceName = (uri: string): string => {
    for (const url of Object.keys(resources)) {
      const found = (resources[url] || []).find((r: any) => r.uri === uri);
      if (found) return found.name;
    }
    return uri;
  };

  const resourceTags: TagItem[] = resourceResults.map((r: any) => ({
    key: r.uri,
    label: getResourceName(r.uri),
  }));

  const attachmentTags: TagItem[] = attachments.map((a: UiAttachment) => ({
    key: a.id,
    label: a.name,
  }));

  /* -------------------------------------------------------------------------- */
  return (
    <form onSubmit={handleSubmit} style={styles.form}>
      {/* TAG ROW  */}
      {(resourceTags.length > 0 || attachmentTags.length > 0) && (
        <div style={styles.tagRow}>
          {resourceTags.length > 0 && (
            <Tags
              items={resourceTags}
              onRemove={async (id) => await removeResourceResult(id)}
            />
          )}
          {attachmentTags.length > 0 && (
            <Tags
              items={attachmentTags}
              onRemove={async (id) => await removeAttachment(id)}
            />
          )}
        </div>
      )}

      {/* FIRST ROW – TEXT INPUT */}
      <TextArea
        ref={textareaRef}
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        placeholder={t("promptPlaceholder")}
        style={styles.textArea}
      />

      {/* SECOND ROW – CONTROLS */}
      <div style={styles.buttonRow}>
        {/* LEFT‑ALIGNED GROUP */}
        <div style={styles.leftGroup}>
          <ServerSelectButton />
          <PromptSelectButton model={model} />
          <ResourceSelectButton />
          <Button
            type="button"
            icon="attachment"
            variant="transparent"
            disabled={disabled}
            onClick={() => fileInputRef.current?.click()}
          />
          <input
            ref={fileInputRef}
            type="file"
            multiple
            style={{ display: "none" }}
            onChange={handleFileChange}
          />
        </div>

        {/* RIGHT‑ALIGNED SEND / STOP */}
        {streaming ? (
          <Button type="button" icon="stop" onClick={onStop} />
        ) : (
          <Button type="submit" disabled={disabled || !canSend} icon="send" />
        )}
      </div>
    </form>
  );
};

// ───────── styles ─────────
const styles: Record<string, React.CSSProperties> = {
  form: {
    maxWidth: 1056,
    margin: "auto",
    display: "flex",
    flexDirection: "column",
    gap: 8,
    width: "100%",
  },
  tagRow: {
    display: "flex",
    gap: 8,
    marginBottom: 4,
    width: "100%",
  },
  textArea: {
    resize: "vertical",
    maxHeight: 120,
    flex: 1,
  },
  buttonRow: {
    display: "flex",
    width: "100%",
    alignItems: "center",
    gap: 8,
  },
  leftGroup: {
    display: "flex",
    gap: 8,
    flex: 1,
  },
};
