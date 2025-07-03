import React, { useState } from "react";
import { useTheme } from "../../../ThemeContext";

export interface ToolResultImageCardProps {
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

export const ToolResultImageCard: React.FC<ToolResultImageCardProps> = ({
  invocation,
  item,
  isError,
}) => {
  const theme = useTheme();
  const src = `data:${item.mimeType};base64,${item.data}`;
  return theme.Card({
    title: invocation.toolName,
    children: <theme.Image src={src} />,
  });
};
