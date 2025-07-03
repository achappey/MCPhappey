import React from "react";
import { useToolInvocations } from "./useToolInvocations";
import { ToolInvocationCard } from "./ToolInvocationCard";
/**
 * Renders a list of all tool invocation activities in the current conversation as cards.
 */
export const ToolInvocationsActivity: React.FC = () => {
  const invocations = useToolInvocations();

  if (!invocations.length) {
    return (
      <div style={{ padding: 16, color: "#888" }}>
        No tool invocations found in this conversation.
      </div>
    );
  }

  // Show newest first
  const reversed = [...invocations].reverse();

  return (
    <div
      style={{ padding: 8, display: "flex", flexDirection: "column", gap: 12 }}
    >
      {reversed.map((inv, i) => (
        <ToolInvocationCard
          key={(inv.toolCallId || inv.msgId || i) + "-inv"}
          invocation={inv}
        />
      ))}
    </div>
  );
};
