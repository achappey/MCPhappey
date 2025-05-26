import * as React from "react";
import { Switch as FluentSwitch } from "@fluentui/react-components";

export const Switch = ({
  id,
  label,
  checked,
  onChange,
  className,
}: {
  id: string;
  label?: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  className?: string;
}): JSX.Element => (
  <FluentSwitch
    id={id}
    checked={checked}
    onChange={(_, data) => onChange(data.checked)}
    className={className}
    label={label}
  />
);
