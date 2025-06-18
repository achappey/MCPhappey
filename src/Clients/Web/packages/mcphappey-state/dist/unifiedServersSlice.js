import { fetchServerList } from "mcphappey-http"; // to be implemented/linked
const uniqBy = (arr, key) => {
    const seen = new Set();
    return arr.filter(item => {
        const k = key(item);
        if (seen.has(k))
            return false;
        seen.add(k);
        return true;
    });
};
export const createUnifiedServersSlice = (set, get) => ({
    servers: [],
    loading: false,
    error: null,
    importList: async (url) => {
        set({ loading: true, error: null });
        try {
            const data = await fetchServerList(url);
            if (data.ok == false) {
                throw new Error(data.error.message);
            }
            //const imported: McpServerWithName[] = Object.entries(data.data || {}).map<McpServerWithName>(
            //([name, details]) => ({ name, ...(details as Omit<McpServerWithName, "name">) })
            //);
            const merged = uniqBy([...get().servers, ...data.data], s => s.name + "|" + s.url);
            set({ servers: merged, loading: false });
        }
        catch (e) {
            set({ error: e?.message || "Failed to import server list", loading: false });
        }
    },
    clearAll: () => set({ servers: [] }),
});
//# sourceMappingURL=unifiedServersSlice.js.map