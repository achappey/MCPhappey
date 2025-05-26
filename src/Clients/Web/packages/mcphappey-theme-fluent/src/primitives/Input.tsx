import * as React from "react";
import type { ComponentProps, JSX } from "react";
import { Input as FluentInput } from "@fluentui/react-components";

export const Input = (props: ComponentProps<"input">): JSX.Element => {
  const { size, ...rest } = props;
  const sizeProp =
    typeof size === "string"
      ? size === "sm" || size === "small"
        ? "small"
        : size === "lg" || size === "large"
        ? "large"
        : "medium"
      : "medium";

  return <FluentInput size={sizeProp} {...(rest as any)} />;
};
