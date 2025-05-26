import { useAppStore } from "./store";
export const useUnifiedServers = () => useAppStore((s) => s.servers);
export const useImportServerList = () => useAppStore((s) => s.importList);
export const useUnifiedServersLoading = () => useAppStore((s) => s.loading);
export const useUnifiedServersError = () => useAppStore((s) => s.error);
//# sourceMappingURL=selectors.js.map