// Chat domain primitives

export type MessageRole = "user" | "assistant" | "system";

export interface ChatMessage {
  id: string;
  role: MessageRole;
  content: React.ReactNode;
  text: string;
  createdAt?: string; // ISO string
  author?: string; // ISO string
  sources?: { title?: string; url: string }[];
  tools?: any[];
  totalTokens?: number;
  temperature?: number;
  copyToClipboard?: () => Promise<void>
  download?: () => void
}

export interface UIMessage {
  id: string;
  role: MessageRole;
  parts: any[];
  metadata?: Record<string, any>
}

export interface Conversation {
  id: string;
  //name: string;
  messages: UIMessage[];
  metadata?: Record<string, any>
}

export interface ToolCallResult {
  isError?: boolean;
  structuredContent?: any
  content: any[]
}
