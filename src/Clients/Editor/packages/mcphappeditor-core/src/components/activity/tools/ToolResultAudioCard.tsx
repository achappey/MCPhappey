import React from "react";
import { useTheme } from "../../../ThemeContext";

export interface ToolResultAudioCardProps {
  invocation: {
    toolName: string;
    toolCallId?: string;
  };
  item: {
    type: "image";
    data: string;
    mimeType: string;
  };
  isError?: boolean;
}

export const ToolResultAudioCard: React.FC<ToolResultAudioCardProps> = ({
  invocation,
  item,
  isError,
}) => {
  const theme = useTheme();

  // Build a data-URL from the base-64 payload.
  const src = `data:${item.mimeType};base64,${item.data}`;

  return theme.Card({
    title: invocation.toolName,
    children: (
      <audio
        controls
        preload="metadata"
        style={{ width: "100%", outline: "none" }}
      >
        <source src={src} type={item.mimeType} />
        Your browser does not support the HTML&nbsp;audio element.
      </audio>
    ),
  });
};
