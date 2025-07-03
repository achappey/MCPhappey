// Server DTOs for MCPHappeditor API

export interface Server {
  name: string;
  instructions?: string;
  // Add other fields as needed from backend DTO
}

export interface Resource {
  uri: string;
  name: string;
  description?: string;
  // Add other fields as needed from backend DTO
}

export interface Prompt {
  name: string;
  description?: string;
  promptTemplate: string;
  arguments?: PromptArgument[];
  // Add other fields as needed from backend DTO
}

export interface PromptArgument {
  name: string;
  required?: boolean;
  description?: string;
  // Add other fields as needed from backend DTO
}