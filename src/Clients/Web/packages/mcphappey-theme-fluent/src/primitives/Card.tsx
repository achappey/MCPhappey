import * as React from "react";
import type { JSX } from "react";
import { Card as FluentCard, CardHeader, CardFooter, CardPreview } from "@fluentui/react-components";

export const Card = ({
  title,
  text,
  actions,
}: {
  title: string;
  text: string;
  actions?: JSX.Element;
}): JSX.Element => (
  <FluentCard>
    <CardHeader header={<span>{title}</span>} />
    <CardPreview>{text}</CardPreview>
    {actions && <CardFooter>{actions}</CardFooter>}
  </FluentCard>
);
