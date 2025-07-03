import React, { useState } from "react";
import { useTheme } from "../../../ThemeContext";
import { ShowToolCallResult } from "../ShowToolCallResult";

export interface ToolResultResourceCardProps {
  invocation: {
    toolName: string;
    toolCallId?: string;
  };
  item: {
    type: "resource";
    resource: {
      mimeType: string;
      uri: string;
      text?: string;
    };
  };
  isError?: boolean;
}

export const ToolResultResourceCard: React.FC<ToolResultResourceCardProps> = ({
  invocation,
  item,
  isError,
}) => {
  const theme = useTheme();
  const [showResource, setShowResource] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(item.resource.uri);
    setCopied(true);
    setTimeout(() => setCopied(false), 1200);
  };

  return (
    <>
      {theme.Card({
        title: invocation.toolName,
        text: item.resource.uri,
        actions: (
          <>
            {theme.Button({
              size: "sm",
              children: "Open",
              onClick: () => window.open(item.resource.uri, "_blank"),
            })}
            {item.resource.text &&
              theme.Button({
                size: "sm",
                children: "View",
                onClick: () => setShowResource(true),
              })}
          </>
        ),
      })}

     
    </>
  );
};

/* {showResource && (
        <ShowToolCallResult
          open={showResource}
          onClose={() => setShowResource(false)}
          result={item.resource.text}
        />
      )}*/