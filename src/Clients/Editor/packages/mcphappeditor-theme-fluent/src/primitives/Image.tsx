import { Image as FluentImage } from "@fluentui/react-components";

export const Image = ({
  src,
  ...rest
}: {
  src?: string;
  fit?: "none" | "center" | "contain" | "cover" | "default";
  shadow?: boolean;
  block?: boolean;
  bordered?: boolean;
  shape?: "circular" | "rounded" | "square";
}): JSX.Element => <FluentImage loading="lazy" src={src} {...rest} />;
