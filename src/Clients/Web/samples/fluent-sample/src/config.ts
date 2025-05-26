/**
 * Build-time injected constant holding the MCP server list URL.
 * esbuild replaces __DEFAULT_MCP_SERVER_LIST_URLS__ with a string literal
 * representing a JSON array via its `define` option (see esbuild.config.js).
 * A hard-coded localhost value remains as a safety fallback for unconfigured builds.
 */
declare const __DEFAULT_MCP_SERVER_LIST_URLS__: string[]; // Now an array

export const DEFAULT_MCP_SERVER_LIST_URLS: string[] =
  __DEFAULT_MCP_SERVER_LIST_URLS__ || ["http://localhost:3001/mcp.json"];
