import * as React from "react";
import { Badge as FluentBadge } from "@fluentui/react-components";

export const Badge = ({
  bg,
  text,
  children,
}: {
  bg?: string;
  text?: string;
  children: React.ReactNode;
}): JSX.Element => (
  <FluentBadge color={(bg as any) == "primary" ? "brand" : (bg as any)}>
    {text ?? children}
  </FluentBadge>
);
