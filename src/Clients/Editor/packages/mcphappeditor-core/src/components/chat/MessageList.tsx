import { useTheme } from "../../ThemeContext";
import { MemoMarkdown } from "../markdown/Markdown";
import { copyMarkdownToClipboard, downloadBase64Image } from "./utils/file";
import { ChatMessageData, useChatMessages } from "./hooks/useChatMessages";
import { useAppStore } from "mcphappeditor-state";
import { ElicitForm } from "./ElicitForm";
import { respondElicit } from "../mcp/McpConnectionsProvider";
import { ChatMessage } from "mcphappeditor-types";

interface MessageListProps {
  showCitations: (items: any[]) => void;
  showToolsDrawer?: (tools: any[]) => void;
}

export const MessageList = ({
  showCitations,
  showToolsDrawer,
}: MessageListProps) => {
  const { Chat } = useTheme();
  const chatMessageData = useChatMessages();
  const elicit = useAppStore((s) => s.elicit);

  var elicitMessages: ChatMessage[] = Object.keys(elicit)
    .filter((a) => !elicit[a][2])
    .map((a) => ({
      id: elicit[a][0],
      role: "assistant",
      author: elicit[a][0],
      isImage: false,
      content: (
        <ElicitForm
          key={elicit[a][0]}
          id={elicit[a][0]}
          params={elicit[a][1].params}
          onRespond={(r) => respondElicit(a, r)}
        />
      ),
      text: JSON.stringify(elicit[a][1].params),
    }));

  const chatMessages = chatMessageData.map((m) => ({
    ...m,
    content: <MemoMarkdown text={m.contentText} />,
    text: m.contentText,
    copyToClipboard: m.isImage
      ? undefined
      : async () => await copyMarkdownToClipboard(m.contentText),
    download: m.isImage
      ? () => {
          // Heuristically find a file extension (data-url or url)
          let url = "";
          let ext = "png";
          if (m.contentText.startsWith("![")) {
            // Try to extract URL from markdown image
            const match = /\]\((.*?)\)/.exec(m.contentText);
            if (match) url = match[1];
            if (url.startsWith("data:")) {
              const mime = url.split(";")[0].split(":")[1];
              ext = mime?.split("/")[1] || "png";
            }
          }
          downloadBase64Image(url, `${m.id}.${ext}`);
        }
      : undefined,
  }));

  return (
    <div
      style={{ flex: 1, overflowY: "auto", overflowX: "hidden", padding: 12 }}
    >
      {chatMessages.length > 0 && (
        <Chat
          onShowSources={showCitations}
          messages={[...chatMessages, ...elicitMessages]}
          onShowTools={showToolsDrawer}
        />
      )}
    </div>
  );
};
