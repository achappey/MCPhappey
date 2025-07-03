import React from "react";
import { ProgressNotificationCard } from "./ProgressNotificationCard";
import { useAppStore } from "mcphappeditor-state";

export const ProgressNotificationsActivity: React.FC = () => {
  const progress = useAppStore((a) => a.progress);

  if (!progress.length) {
    return <div className="p-3 text-muted">No progress</div>;
  }
  progress.reverse();
  return (
    <div
      className="p-3"
      style={{ display: "flex", flexDirection: "column", gap: 8 }}
    >
      {progress.map((n: any, i: number) => (
        <ProgressNotificationCard key={i} notif={n} />
      ))}
    </div>
  );
};
