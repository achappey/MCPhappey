export * from "./mcpTester";
export * from "./mcpConnector";
export {
    CreateMessageRequest,
    CreateMessageRequestSchema, LoggingMessageNotification,
    ProgressNotification, type ResourceTemplate, ElicitRequest,
    ElicitResult, type CallToolResult, type ReadResourceResult,
    CreateMessageResult, CreateMessageResultSchema, type Prompt,
    type ServerCapabilities, type Tool
} from "@modelcontextprotocol/sdk/types.js";
import type { Resource as BaseResource } from "@modelcontextprotocol/sdk/types.js";
export { Client } from "@modelcontextprotocol/sdk/client/index.js";

export type Resource = BaseResource & {
    annotations?: ResourceAnnotations;
};

export type ResourceAnnotations = {
    audience?: ResourceAudience[];
    priority?: number;
};

export type ResourceAudience = "assistant" | "user";