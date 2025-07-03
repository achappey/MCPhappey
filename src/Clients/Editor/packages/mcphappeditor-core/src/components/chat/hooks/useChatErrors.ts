import { useAppStore } from "mcphappeditor-state";

// Returns: { errors, dismissError }
export function useChatErrors() {
  const errors = useAppStore((s) => s.chatErrors);
  const dismissError = useAppStore((s) => s.dismissChatError);
  return { errors, dismissError };
}
