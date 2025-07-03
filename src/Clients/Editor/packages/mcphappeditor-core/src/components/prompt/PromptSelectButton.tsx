import { useState, useMemo, useEffect } from "react";
import { useTheme } from "../../ThemeContext";
import { PromptCard } from "./PromptCard";
import type { Prompt } from "@modelcontextprotocol/sdk/types.js";
import { PromptArgumentsModal } from "./PromptArgumentsModal";
import { useTranslation } from "mcphappeditor-i18n";
import { CancelButton } from "../common/CancelButton";
import { useAppStore } from "mcphappeditor-state";
import { useExecutePrompt } from "./useExecutePrompt";

type PromptWithSource = Prompt & { _url: string; text?: string };

type PromptSelectButtonProps = {
  model?: string;
};

export const PromptSelectButton = ({ model }: PromptSelectButtonProps) => {
  const { Button } = useTheme();

  const prompts = useAppStore((s) => s.prompts);
  const selected = useAppStore((s) => s.selected);
  const servers = useAppStore((s) => s.servers);
  const refreshPrompts = useAppStore((s) => s.refreshPrompts);

  // Map selected server names to URLs, then flatten prompts by URL
  const allPrompts: PromptWithSource[] = useMemo(
    () =>
      selected.flatMap((name: string) => {
        const url = servers[name]?.url;
        return url && Array.isArray(prompts[url])
          ? prompts[url].map((p: any) => ({ ...p, _url: url }))
          : [];
      }),
    [selected, servers, prompts]
  );

  const hasPrompts: boolean = useMemo(
    () =>
      selected
        .map((name: string) => {
          const url = servers[name]?.url;
          return url != null && Array.isArray(prompts[url]);
        })
        .some((a) => a == true),
    [selected, servers, prompts]
  );

  const [open, setOpen] = useState(false);
  const [argumentPrompt, setArgumentPrompt] = useState<PromptWithSource | null>(
    null
  );

  const executePrompt = useExecutePrompt(model);

  // Lazy-load prompts when modal opens
  useEffect(() => {
    if (!open) return;
    selected.forEach((name: string) => {
      const url = servers[name]?.url;
      if (url && (!prompts[url] || prompts[url].length === 0)) {
        void refreshPrompts(url);
      }
    });
  }, [open, selected, servers, prompts, refreshPrompts]);

  return (
    <>
      <Button
        type="button"
        disabled={!hasPrompts}
        variant="transparent"
        icon="prompts"
        onClick={() => setOpen(true)}
        title={"Insert Prompt"}
      ></Button>

      {open && (
        <PromptSelectModal
          prompts={allPrompts}
          onPromptClick={(p) => {
            if (p.arguments && p.arguments.length > 0) {
              setArgumentPrompt(p);
            } else {
              void executePrompt(p);
            }
            setOpen(false);
          }}
          onHide={() => setOpen(false)}
        />
      )}

      {argumentPrompt && (
        <PromptArgumentsModal
          prompt={argumentPrompt}
          model={model}
          onHide={() => setArgumentPrompt(null)}
        />
      )}
    </>
  );
};

type PromptSelectModalProps = {
  prompts: PromptWithSource[];
  onPromptClick: (p: PromptWithSource) => void;
  onHide: () => void;
};

const PromptSelectModal = ({
  prompts,
  onPromptClick,
  onHide,
}: PromptSelectModalProps) => {
  const { Modal } = useTheme();
  const { t } = useTranslation();

  return (
    <Modal
      show={true}
      onHide={onHide}
      actions={<CancelButton onClick={onHide} />}
      title={t("mcp.prompts")}
    >
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: 8,
        }}
      >
        {prompts.map((prompt, idx) => (
          <PromptCard
            key={prompt.name + idx}
            prompt={prompt}
            onSelect={() => onPromptClick(prompt as PromptWithSource)}
          />
        ))}
      </div>
    </Modal>
  );
};
