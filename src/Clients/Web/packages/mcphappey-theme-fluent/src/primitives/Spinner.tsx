import * as React from "react";
import { Spinner as FluentSpinner } from "@fluentui/react-components";

export const Spinner = ({
  size = "tiny",
  className,
}: {
  size?: string;
  className?: string;
}): JSX.Element => (
  <FluentSpinner
    size={
      size === "sm" || size === "tiny"
        ? "tiny"
        : size === "xs" || size === "extra-small"
        ? "extra-small"
        : size === "md" || size === "medium"
        ? "medium"
        : size === "lg" || size === "large"
        ? "large"
        : "tiny"
    }
    className={className}
  />
);
