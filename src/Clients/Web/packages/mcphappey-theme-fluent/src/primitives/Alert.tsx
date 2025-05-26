import * as React from "react";
import { Alert as FluentAlert } from "@fluentui/react-alert";

export const Alert = ({
  variant,
  className,
  children,
}: {
  variant: string;
  className?: string;
  children: React.ReactNode;
}): JSX.Element => (
  <FluentAlert
    appearance={
      variant === "danger" || variant === "error"
        ? "primary"
        : variant === "warning"
        ? "inverted"
        : undefined
    }
    className={className}
  >
    {children}
  </FluentAlert>
);
