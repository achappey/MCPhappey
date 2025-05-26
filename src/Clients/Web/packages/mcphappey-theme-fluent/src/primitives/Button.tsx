import * as React from "react";
import type { ComponentProps, JSX } from "react";
import { Button as FluentButton } from "@fluentui/react-components";

export const Button = ({
  variant = "primary",
  size = "medium",
  ...rest
}: ComponentProps<"button"> & {
  variant?: string;
  size?: string;
}): JSX.Element => (
  <FluentButton
    appearance={
      variant === "primary"
        ? "primary"
        : variant === "secondary"
        ? "secondary"
        : variant === "outline"
        ? "outline"
        : "transparent"
    }
    size={
      size === "sm" || size === "small"
        ? "small"
        : size === "lg" || size === "large"
        ? "large"
        : "medium"
    }
    {...(rest as any)}
  />
);
