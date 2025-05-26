import * as React from "react";
import { Textarea as FluentTextarea } from "@fluentui/react-components";

export const TextArea = ({
  rows,
  value,
  onChange,
  style,
  className,
}: {
  rows?: number;
  value: string;
  onChange: (value: string) => void;
  style?: React.CSSProperties;
  className?: string;
}): JSX.Element => (
  <FluentTextarea
    rows={rows}
    value={value}
    onChange={(_, data) => onChange(data.value)}
    style={style}
    className={className}
  />
);
