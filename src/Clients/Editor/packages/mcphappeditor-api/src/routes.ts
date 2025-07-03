// REST endpoint path builders for MCPHappeditor API

export const routes = {
  servers: () => '/api/servers',
  server: (id: string) => `/api/servers/${id}`,
  resources: (mcpId: string) => `/api/servers/${mcpId}/resources`,
  resource: (mcpId: string, id: string) =>
    `/api/servers/${mcpId}/resources/${encodeURIComponent(id)}`,
  prompts: (mcpId: string) => `/api/servers/${mcpId}/prompts`,
  prompt: (mcpId: string, id: string) =>
    `/api/servers/${mcpId}/prompts/${encodeURIComponent(id)}`
};