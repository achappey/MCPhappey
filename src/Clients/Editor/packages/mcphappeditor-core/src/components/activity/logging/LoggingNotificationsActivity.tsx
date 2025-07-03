import React from "react";
import { useAppStore } from "mcphappeditor-state";
import { LoggingNotificationCard } from "./LoggingNotificationCard";

export const LoggingNotificationsActivity: React.FC = () => {
  const notifications = useAppStore((a) => a.notifications);

  if (!notifications.length) {
    return <div className="p-3 text-muted">No notifications</div>;
  }
  notifications.reverse();
  return (
    <div
      className="p-3"
      style={{ display: "flex", flexDirection: "column", gap: 8 }}
    >
      {notifications.map((n, i: number) => (
        <LoggingNotificationCard key={i} notif={n} />
      ))}
    </div>
  );
};
