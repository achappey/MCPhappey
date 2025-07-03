import type { JSX } from "react";
import {
  Chat as FluentChat,
  ChatMessage,
  ChatMyMessage,
} from "@fluentui-contrib/react-chat";
import { ChatMessage as Message } from "mcphappeditor-types";
import { Badge, Button, Tooltip } from "@fluentui/react-components";
import {
  ArrowDownloadRegular,
  CloudLinkRegular,
  CodeTextRegular,
  CopyRegular,
  TemperatureRegular,
  ToolboxRegular,
} from "@fluentui/react-icons";

const uniq = (arr?: { url: string }[]) =>
  arr ? Array.from(new Map(arr.map((s) => [s.url, s])).values()) : [];

const Actions = ({
  copy,
  sources,
  tools,
  download,
  onOpenSources,
  onOpenTools,
  totalTokens,
  temperature,
}: {
  copy?: () => void;
  download?: () => void;
  sources?: { title?: string; url: string }[];
  tools?: any[];
  totalTokens?: number;
  temperature?: number;
  onOpenSources: () => void;
  onOpenTools: () => void;
}) => (
  <div
    style={{ height: 16, paddingTop: 8, display: "flex", alignItems: "center" }}
  >
    {copy != undefined ? (
      <Button
        icon={{ children: <CopyRegular fontSize={32} /> }}
        onClick={copy}
        size="medium"
        appearance="subtle"
      />
    ) : null}
    {download != undefined ? (
      <Button
        icon={{ children: <ArrowDownloadRegular fontSize={32} /> }}
        onClick={download}
        size="medium"
        appearance="subtle"
      />
    ) : null}
    {sources?.length && sources?.length > 0 ? (
      <Button
        icon={{ children: <CloudLinkRegular fontSize={32} /> }}
        onClick={onOpenSources}
        size="medium"
        appearance="subtle"
      >
        {sources.length} source{sources.length > 1 ? "s" : ""}
      </Button>
    ) : null}
    {tools?.length && tools.length > 0 ? (
      <Button
        icon={{ children: <ToolboxRegular fontSize={32} /> }}
        onClick={onOpenTools}
        size="medium"
        appearance="subtle"
      >
        {tools.length} tool{tools.length > 1 ? "s" : ""}
      </Button>
    ) : null}
    {temperature != undefined && (
      <Tooltip relationship="label" content={"Temperature"}>
        <Badge
          icon={{ children: <TemperatureRegular fontSize={16} /> }}
          size="medium"
          color="subtle"
          appearance="ghost"
        >
          {temperature}
        </Badge>
      </Tooltip>
    )}
    {totalTokens && totalTokens > 0 && (
      <Tooltip relationship="label" content={"Total tokens"}>
        <Badge
          icon={{ children: <CodeTextRegular fontSize={16} /> }}
          size="medium"
          color="subtle"
          appearance="ghost"
        >
          {totalTokens}
        </Badge>
      </Tooltip>
    )}
  </div>
);

export const Chat = ({
  messages,
  // copyToClipboard,
  onShowSources,
  onShowTools,
}: {
  messages?: Message[];
  // copyToClipboard: any;
  onShowSources?: (sources: { title?: string; url: string }[]) => void;
  onShowTools?: (tools: any[]) => void;
}): JSX.Element => {
  return (
    <FluentChat>
      <style>
        {`
        .fui-ChatMessage {
          margin-left: initial;
        }
      `}
      </style>
      {messages?.map(
        ({
          id,
          role,
          author,
          content,
          createdAt,
          // text,
          copyToClipboard,
          download,
          sources,
          tools,
          totalTokens,
          temperature,
        }) => {
          const uniqueSources = uniq(sources);
          const MessageComponent =
            role === "user" ? ChatMyMessage : ChatMessage;
          return (
            <div key={id}>
              <MessageComponent
                author={author}
                timestamp={createdAt}
                reactions={
                  <Actions
                    copy={copyToClipboard}
                    download={download}
                    sources={uniqueSources}
                    tools={tools}
                    totalTokens={totalTokens}
                    temperature={temperature}
                    onOpenSources={() => onShowSources?.(uniqueSources)}
                    onOpenTools={() => onShowTools?.(tools ?? [])}
                  />
                }
              >
                {content}
              </MessageComponent>
            </div>
          );
        }
      )}
    </FluentChat>
  );
};
