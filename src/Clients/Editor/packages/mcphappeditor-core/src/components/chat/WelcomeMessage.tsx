import React, { useEffect, useState } from "react";
import { useTheme } from "../../ThemeContext";
import { useAppStore } from "mcphappeditor-state";
import { useTranslation } from "mcphappeditor-i18n";

interface WelcomeMessageProps {
  status: string;
  selectedConversationId: string | null;
}

export const WelcomeMessage: React.FC<WelcomeMessageProps> = ({
  status,
  selectedConversationId,
}) => {
  const chatAppMcp = useAppStore((s) => s.chatAppMcp);
  const { Skeleton } = useTheme();
  const { i18n } = useTranslation();
  const [welcomeMessage, setWelcomeMessage] = useState<string | undefined>(
    undefined
  );

  useEffect(() => {
    if (!selectedConversationId && chatAppMcp && status === "ready") {
      const handleResponse = (res: any) => {
        const text =
          typeof res.content?.[0]?.text === "string"
            ? res.content[0]?.text ?? ""
            : "";
        setWelcomeMessage(text);
      };

      chatAppMcp
        .callTool({
          name: "ChatApp_ExecuteGenerateWelcomeMessage",
          arguments: {
            language: i18n.language,
          },
        })
        .then(handleResponse);
    } else {
      setWelcomeMessage(undefined);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status, selectedConversationId, chatAppMcp]);

  return (
    <>
      {welcomeMessage ? (
        <h1>{welcomeMessage}</h1>
      ) : (
        <Skeleton
          width={350}
          height={36}
          style={{
            lineHeight: 36,
            marginBlockEnd: "0.67em",
            marginBlockStart: "0.67em",
            display: "inline-block",
            boxSizing: "border-box",
          }}
        />
      )}
    </>
  );
};
