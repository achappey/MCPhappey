import type { ComponentProps, JSX } from "react";
import { Field, Input as FluentInput } from "@fluentui/react-components";
import { useDarkMode } from "usehooks-ts";

type ThemedInputProps = ComponentProps<"input"> & {
  label?: string;
  hint?: string;
};

export const Input = (props: ThemedInputProps): JSX.Element => {
  const { size, style, label, required, hint, ...rest } = props;
  const { isDarkMode } = useDarkMode();

  const sizeProp =
    typeof size === "string"
      ? size === "sm" || size === "small"
        ? "small"
        : size === "lg" || size === "large"
        ? "large"
        : "medium"
      : "medium";

  const inputElement = (
    <FluentInput
      style={{ backgroundColor: isDarkMode ? "#141414" : undefined, ...style }}
      size={sizeProp}
      {...(rest as any)}
    />
  );

  // Only wrap in Field if label is given
  return label ? (
    <Field label={label} hint={hint} required={required}>
      {inputElement}
    </Field>
  ) : (
    inputElement
  );
};
