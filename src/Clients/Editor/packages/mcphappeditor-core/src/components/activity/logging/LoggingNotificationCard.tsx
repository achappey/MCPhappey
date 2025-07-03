import React from "react";
import { useTheme } from "../../../ThemeContext";
import { Markdown } from "../../markdown/Markdown";

export interface LoggingNotificationCardProps {
  notif: any;
}

export const LoggingNotificationCard: React.FC<
  LoggingNotificationCardProps
> = ({ notif }) => {
  const { Card } = useTheme();
  const level = String(notif.level || notif.type || "Notification");
  const message = String(notif.data || "");

  return (
    <Card title={notif.level}>
      <Markdown text={message} />
    </Card>
  );
};
