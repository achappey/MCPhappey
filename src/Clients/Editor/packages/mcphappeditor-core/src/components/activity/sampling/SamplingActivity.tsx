import React from "react";
import { SamplingCard } from "./SamplingCard";
import { useAppStore } from "mcphappeditor-state";

export const SamplingActivity: React.FC = () => {
  const sampling = useAppStore((a) => a.sampling);

  var items = Object.keys(sampling)
    .map((t) => sampling[t])
    .reverse();

  if (!items.length) {
    return <div className="p-3 text-muted">No sampling</div>;
  }

  return (
    <div
      className="p-3"
      style={{ display: "flex", flexDirection: "column", gap: 8 }}
    >
      {items.map((n, i: number) => (
        <SamplingCard key={i} notif={n} />
      ))}
    </div>
  );
};
