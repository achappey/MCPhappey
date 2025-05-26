import { useAppStore } from "./store";
import { McpServerWithName } from "mcphappey-types";

export const useUnifiedServers = (): McpServerWithName[] =>
  useAppStore((s) => s.servers);

export const useImportServerList = (): (url: string) => Promise<void> =>
  useAppStore((s) => s.importList);

export const useUnifiedServersLoading = (): boolean =>
  useAppStore((s) => s.loading);

export const useUnifiedServersError = (): string | null =>
  useAppStore((s) => s.error);
