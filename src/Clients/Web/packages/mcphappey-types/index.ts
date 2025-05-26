export * from "./src/theme";

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

/**
 * Capabilities summary for an MCP server.
 */
export interface McpCapabilitySummary {
  prompts?: boolean;
  resources?: boolean;
  tools?: boolean;
  [key: string]: any;
}

/**
 * Information about a prompt provided by the server.
 */
export interface McpPromptInfo {
  name: string;
  description?: string;
  arguments?: McpPromptArgument[]
  [key: string]: any;
}

export interface McpPromptArgument {
  name: string;
  description?: string;
  required?: boolean;
}

/**
 * Information about a resource provided by the server.
 */
export interface McpResourceInfo {
  id: string;
  name: string;
  type?: string;
  description?: string;
  [key: string]: any;
}

/**
 * Information about a tool provided by the server.
 */
export interface McpToolInfo {
  name: string;
  description?: string;
  inputSchema?: any;
  outputSchema?: any;
  [key: string]: any;
}
