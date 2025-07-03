import React from "react";
import { useChatErrors } from "./hooks/useChatErrors";
import { useTheme } from "../../ThemeContext";

export function ChatErrors() {
  const { errors, dismissError } = useChatErrors();
  const { Alert } = useTheme();

  if (!errors?.length) return null;

  return (
    <>
      {errors.map((e) => (
        <Alert
          key={e}
          variant={"error"}
          onDismiss={() => dismissError(e)}
          title={"Error"}
        >
          {e}
        </Alert>
      ))}
    </>
  );
}
