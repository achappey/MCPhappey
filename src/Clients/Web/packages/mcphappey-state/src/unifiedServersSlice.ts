import { StateCreator } from "zustand";
import { McpServerWithName } from "mcphappey-types";
import { fetchServerList } from "mcphappey-http"; // to be implemented/linked

export type UnifiedServersSlice = {
    servers: McpServerWithName[];
    loading: boolean;
    error: string | null;
    importList: (url: string) => Promise<void>;
    clearAll: () => void;
};

const uniqBy = <T, K>(arr: T[], key: (item: T) => K): T[] => {
    const seen = new Set<K>();
    return arr.filter(item => {
        const k = key(item);
        if (seen.has(k)) return false;
        seen.add(k);
        return true;
    });
};

export const createUnifiedServersSlice: StateCreator<
    UnifiedServersSlice,
    [],
    [],
    UnifiedServersSlice
> = (set, get) => ({
    servers: [],
    loading: false,
    error: null,
    importList: async (url: string) => {
        set({ loading: true, error: null });
        try {
            const data = await fetchServerList(url);
            if (data.ok == false) {
                throw new Error(data.error.message)
            }
            //const imported: McpServerWithName[] = Object.entries(data.data || {}).map<McpServerWithName>(
            //([name, details]) => ({ name, ...(details as Omit<McpServerWithName, "name">) })
            //);
            const merged = uniqBy(
                [...get().servers, ...data.data],
                s => s.name + "|" + s.url
            );
            set({ servers: merged, loading: false });
        } catch (e: any) {
            set({ error: e?.message || "Failed to import server list", loading: false });
        }
    },
    clearAll: () => set({ servers: [] }),
});
