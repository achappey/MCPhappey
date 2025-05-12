/**
 * Build-time injected constant holding the MCP server list URL.
 * esbuild replaces __MCP_SERVERS_API_URL__ with a string literal via its
 * `define` option (see esbuild.config.js). A hard-coded localhost value
 * remains as a safety fallback for unconfigured builds.
 */
declare const __MCP_SERVERS_API_URL__: string;

export const MCP_SERVERS_API_URL: string =
  __MCP_SERVERS_API_URL__ || "http://localhost:3001/mcp.json";
