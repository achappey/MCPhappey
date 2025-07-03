import * as React from "react";
import type { JSX } from "react";
import {
  Card as FluentCard,
  CardHeader,
  CardFooter,
  CardPreview,
  tokens,
} from "@fluentui/react-components";
import { useDarkMode } from "usehooks-ts";

export const Card = ({
  title,
  text,
  size,
  children,
  description,
  style,
  actions,
  headerActions,
}: {
  title: string;
  text?: string;
  description?: string,
  size?: any;
  children?: React.ReactNode;
  style?: React.CSSProperties;
  actions?: JSX.Element;
  headerActions?: JSX.Element;
}): JSX.Element => {
  const { isDarkMode } = useDarkMode();
  const previewStyle =
    size == "small"
      ? { paddingLeft: 8, paddingRight: 8 }
      : { paddingLeft: 12, paddingRight: 12 };

  return (
    <FluentCard
      size={size}
      style={{
        backgroundColor:
          isDarkMode && !style?.backgroundColor
            ? tokens.colorNeutralBackground2
            : style?.backgroundColor,
      }}
    >
      <CardHeader header={title} description={description} action={headerActions} />
      <CardPreview style={previewStyle}>{children ?? text}</CardPreview>
      {actions && <CardFooter>{actions}</CardFooter>}
    </FluentCard>
  );
};
