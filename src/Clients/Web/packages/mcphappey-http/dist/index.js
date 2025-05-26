// HTTP utilities and fetch helpers for MCP Happey apps
// Add fetch wrappers, error handling, and helpers here
export function fetchJson(url, options) {
    return fetch(url, options).then(res => {
        if (!res.ok)
            throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        return res.json();
    });
}
//# sourceMappingURL=index.js.map