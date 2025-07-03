import React, { useState } from "react";
import { useTheme } from "../../../ThemeContext";
import { ShowToolCallResult } from "../ShowToolCallResult";

export interface ToolInvocationCardProps {
  invocation: {
    type: string;
    input?: string;
    role: string;
    state?: string;
    output?: any;
    toolCallId?: string;
  };
}

export const ToolInvocationCard: React.FC<ToolInvocationCardProps> = ({
  invocation,
}) => {
  const [showArgs, setShowArgs] = useState(false);
  const { Card, Button, Spinner } = useTheme();
  let argsObj: any = {};
  try {
    argsObj = invocation.input ? JSON.parse(invocation.input) : {};
  } catch {
    argsObj = invocation.input || {};
  }
  const argsPreview = JSON.stringify(argsObj, null, 2);

  return (
    <Card
      title={invocation.type.replace("tool-", "")}
      headerActions={
        <>{invocation.state?.startsWith("input-") && <Spinner size="small" />}</>
      }
      actions={
        <>
          {invocation.state == "output-available" && (
            <Button
              icon="eye"
              size="small"
              onClick={() => setShowArgs(true)}
            >
            </Button>
          )}
        </>
      }
    >
      <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
        <div>
          <pre
            style={{
              whiteSpace: "pre-wrap",
              wordBreak: "break-all",
              margin: 0,
              fontSize: 12,
            }}
          >
            {argsPreview}
          </pre>
        </div>
        {showArgs && (
          <ShowToolCallResult
            open={showArgs}
            onClose={() => setShowArgs(false)}
            result={invocation.output}
          />
        )}
      </div>
    </Card>
  );
};
