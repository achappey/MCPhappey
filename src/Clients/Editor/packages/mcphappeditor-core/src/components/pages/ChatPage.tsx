import { useEffect, useState } from "react";
import { ChatPanel } from "../chat/ChatPanel";
import { ChatHeader } from "../chat/ChatHeader";
import { useAppStore } from "mcphappeditor-state";
import { CitationDrawer } from "../citations/CitationDrawer";
import { useParams } from "react-router";
import { ToolDrawer } from "../tools/ToolDrawer";
import { ActivityDrawer } from "../activity/ActivityDrawer";
import { DisclaimerBar } from "../chat/DisclaimerBar";

export const ChatPage = () => {
  const { conversationId } = useParams<{ conversationId?: string }>();
  const [selectedModel, setSelectedModel] = useState<string | undefined>(
    "gpt-4.1"
  );
  const [sources, setSources] = useState<any[] | undefined>(undefined);
  const [toolsForMessage, setToolsForMessage] = useState<any[] | undefined>(
    undefined
  );
  const setActivities = useAppStore((a) => a.setActivities);
  const selectConversation = useAppStore((a) => a.selectConversation);

  useEffect(() => {
    if (conversationId) {
      selectConversation(conversationId);
    } else {
      selectConversation(null);
    }
    setActivities(false);
  }, [conversationId, setActivities]);

  const showToolsDrawer = (tools: any[]) => setToolsForMessage(tools);

  return (
    <div
      style={{
        height: "100dvh",
        minHeight: 0,
        minWidth: 0,
        display: "flex",
        flexDirection: "column",
      }}
    >
      <ChatHeader value={selectedModel} onChange={setSelectedModel} />

      {/* Main row: chat + inline drawer */}
      <div
        style={{
          flex: 1,
          minHeight: 0,
          minWidth: 0,
          display: "flex",
          flexDirection: "row",
        }}
      >
        {/* Chat column */}
        <div
          style={{
            flex: 1,
            minHeight: 0,
            minWidth: 0,
            display: "flex",
            flexDirection: "column",
          }}
        >
          <ChatPanel
            showCitations={setSources}
            showToolsDrawer={showToolsDrawer}
            model={selectedModel!}
          />
        </div>

        <CitationDrawer
          open={sources != undefined}
          sources={sources ?? []}
          onClose={() => setSources(undefined)}
        />

        <ToolDrawer
          open={toolsForMessage != undefined}
          tools={toolsForMessage ?? []}
          onClose={() => setToolsForMessage(undefined)}
        />

        <ActivityDrawer />
      </div>
      <DisclaimerBar />
    </div>
  );
};
