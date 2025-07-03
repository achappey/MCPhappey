import { get, set } from "idb-keyval";
import type { Conversation, UIMessage } from "mcphappeditor-types/src/chat";
import type { ConversationStore } from "../types";

const DB_KEY = "aihappey_conversations_v1";

async function load(): Promise<Conversation[]> {
  if (typeof window === "undefined") return [];
  try {
    return (await get(DB_KEY)) ?? [];
  } catch {
    return [];
  }
}

async function save(list: Conversation[]) {
  if (typeof window !== "undefined")
    await set(DB_KEY, list);
}

export class IndexedDBConversationStore implements ConversationStore {
  readonly kind = "local";
  private data: Conversation[] = [];
  private loaded = false;

  private async ensureLoaded() {
    if (!this.loaded) {
      this.data = await load();
      this.loaded = true;
    }
  }

  private async commit() {
    await save(this.data);
  }

  list = async (): Promise<Conversation[]> => {
    await this.ensureLoaded();
    return this.data;
  };

  get = async (id: string): Promise<Conversation | undefined> => {
    await this.ensureLoaded();
    return this.data.find((c) => c.id === id);
  };

  create = async (name: string, temperature?: number): Promise<Conversation> => {
    await this.ensureLoaded();
    const conv: Conversation = {
      id: crypto.randomUUID(),
      messages: [],
      metadata: {
        name,
        temperature: typeof temperature === "number" ? temperature : 1
      }
    };
    this.data = [conv, ...this.data];
    await this.commit();
    return conv;
  };

  rename = async (id: string, name: string): Promise<void> => {
    await this.ensureLoaded();
    this.data = this.data.map((c) =>
      c.id === id ? {
        ...c, metadata: {
          ...c.metadata,
          name: name
        }
      } : c
    );
    await this.commit();
  };

  remove = async (id: string): Promise<void> => {
    await this.ensureLoaded();
    this.data = this.data.filter((c) => c.id !== id);
    await this.commit();
  };

  addMessage = async (cid: string, msg: UIMessage): Promise<void> => {
    await this.ensureLoaded();
    this.data = this.data.map((c) =>
      c.id === cid ? { ...c, messages: [...c.messages, msg] } : c
    );
    await this.commit();
  };

  updateMessage = async (
    cid: string,
    mid: string,
    up: Partial<UIMessage>
  ): Promise<void> => {
    await this.ensureLoaded();
    let foundConversation = false;
    let foundMessage = false;

    this.data = this.data.map((c) => {
      if (c.id !== cid) return c;
      foundConversation = true;
      const messages = c.messages.map((m) => {
        if (m.id !== mid) return m;
        foundMessage = true;
        return { ...m, ...up };
      });
      return { ...c, messages };
    });

    if (!foundConversation) {
      throw new Error(`Conversation ${cid} not found`);
    }
    if (!foundMessage) {
      throw new Error(`Message ${mid} not found in conversation ${cid}`);
    }

    await this.commit();
  };
}
