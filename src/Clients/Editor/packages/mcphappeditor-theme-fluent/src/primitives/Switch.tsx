import * as React from "react";
import { Field, Switch as FluentSwitch } from "@fluentui/react-components";

export const Switch = ({
  id,
  label,
  checked,
  onChange,
  required,
  hint,
  className,
}: {
  id: string;
  label?: string;
  hint?: string;
  checked: boolean;
  required?: boolean;
  onChange: (checked: boolean) => void;
  className?: string;
}): JSX.Element => {
  const switchElement = (
    <FluentSwitch
      id={id}
      required={required}
      checked={checked}
      onChange={(_, data) => onChange(data.checked)}
      className={className}
      label={label}
    />
  );

  return label ? (
    <Field  hint={hint} required={required}>
      {switchElement}
    </Field>
  ) : (
    switchElement
  );
};
