import { useMemo } from "react";
import { buildSystemMessage } from "mcphappeditor-ai/src/utils/buildSystemMessage";
import { useAppStore } from "mcphappeditor-state";
import { useAccount } from "mcphappeditor-auth";
import { useShallow } from "zustand/shallow";

export function useSystemMessage() {
  const { mcpInstructions, resources, resourceTemplates } = useAppStore(
    useShallow((state) => ({
      mcpInstructions: state.mcpInstructions,
      resources: state.resources,
      resourceTemplates: state.resourceTemplates,
    }))
  );
  const systemInstructions = useAppStore((s) => s.systemInstructions);
  const account = useAccount();

  const systemMsg = useMemo(() => {
    return buildSystemMessage(
      mcpInstructions,
      resources,
      resourceTemplates,
      systemInstructions,
      account
        ? {
            username: account?.username,
            name: account?.name,
            id: account?.localAccountId,
            tenantId: account?.tenantId,
          }
        : undefined
    );
  }, [
    mcpInstructions,
    resources,
    resourceTemplates,
    systemInstructions,
    account?.username,
    account?.name,
    account?.localAccountId,
    account?.tenantId,
  ]);
  return systemMsg;
}
