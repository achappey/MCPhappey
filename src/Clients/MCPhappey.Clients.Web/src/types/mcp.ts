/**
 * Represents a single MCP server definition.
 */
export interface McpServer {
  type: string;
  url: string;
  headers?: Record<string, string>;
  [key: string]: any; // Allow for additional properties specific to server types
}

/**
 * Expected structure of the JSON response from an MCP server list URL.
 */
export interface McpServerListResponse {
  servers: {
    [name: string]: McpServer;
  };
}

/**
 * Represents an MCP server with its assigned name from the list.
 */
export interface McpServerWithName extends McpServer {
  name: string;
}
