import React, { useState } from "react";
import { useTheme } from "../../../ThemeContext";
import { ShowToolCallResult } from "../ShowToolCallResult";

export interface ToolResultTextCardProps {
  invocation: {
    type: string;
    toolCallId?: string;
  };
  item: {
    type: "text";
    text: string;
  };
  isError?: boolean;
}

export const ToolResultTextCard: React.FC<ToolResultTextCardProps> = ({
  invocation,
  item,
  isError,
}) => {
  const { Button, Card } = useTheme();
  const [showText, setShowText] = useState(false);

  const preview =
    item.text.length > 120 ? item.text.slice(0, 120) + "..." : item.text;

  const jsonString = JSON.stringify(item.text);
  const bytes = new TextEncoder().encode(jsonString).length;
  const kilobytes = bytes / 1024;

  return (
    <>
      <Card
        title={invocation.type}
        size="small"
        text={`Size: ${kilobytes.toFixed(2)} KB`}
        actions={
          <>
            <Button
              onClick={() => setShowText(true)}
              size="small"
              icon="eye"
            ></Button>
          </>
        }
      ></Card>
    </>
  );
  /*

  {showText && (
        <ShowToolCallResult
          open={showText}
          onClose={() => setShowText(false)}
          result={item.text}
        />
      )}

      
  return theme.Card({
    title: invocation.type,
    actions: <><Button></Button></>
    children: (
      <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
        <div>
          <div style={{ fontSize: 13, color: "#666", marginBottom: 2 }}>
            Result (preview):
          </div>
          <pre
            style={{
              maxWidth: 340,
              whiteSpace: "pre-wrap",
              wordBreak: "break-all",
              margin: 0,
              fontSize: 13,
            }}
          >
            {preview}
          </pre>
        </div>
        <div
          style={{ display: "flex", justifyContent: "flex-end", marginTop: 4 }}
        >
          {item.text.length > 0 &&
            (theme.Button ? (
              theme.Button({
                size: "sm",
                children: "Show full text",
                onClick: () => setShowText(true),
              })
            ) : (
              <button
                style={{ fontSize: 12 }}
                onClick={() => setShowText(true)}
              >
                Show full text
              </button>
            ))}
        </div>
        {showText && (
          <ShowJsonModal
            open={showText}
            onClose={() => setShowText(false)}
            title="Tool Result Text"
            json={item.text}
          />
        )}
      </div>
    ),
  });*/
};
