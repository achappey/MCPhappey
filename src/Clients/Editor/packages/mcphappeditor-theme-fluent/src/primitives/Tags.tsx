import { TagGroup } from "mcphappeditor-types";
import React from "react";
import { TagGroup as FTagGroup, Tag } from "@fluentui/react-components";

export const Tags = ({ items, onRemove }: TagGroup): React.ReactNode => (
  <div>
    <FTagGroup
      aria-label="Fluent tag group"
      onDismiss={onRemove ? (_e, data) => onRemove(data.value) : undefined}
      style={{ display: "flex", flexWrap: "wrap", gap: 8, marginBottom: 8 }}
    >
      {items?.map((tag) => (
        <Tag
          key={tag.key}
          value={tag.key}
          dismissible={!!onRemove}
          dismissIcon={onRemove ? { "aria-label": "Remove" } : undefined}
        >
          {tag.label}
        </Tag>
      ))}
    </FTagGroup>
  </div>
);
