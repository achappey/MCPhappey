import * as React from "react";
import type { ComponentProps, JSX } from "react";
import { Button as FluentButton, Hamburger } from "@fluentui/react-components";
import type { IconToken } from "mcphappeditor-types";
import {
  AddRegular,
  EditRegular,
  DeleteRegular,
  SendRegular,
  SettingsRegular,
  ChevronDownRegular,
  ChevronUpRegular,
  DocumentRegular,
  PromptRegular,
  PlugConnectedSettingsRegular,
  BotFilled,
  DoorArrowRightRegular,
  PeopleSettingsRegular,
  PersonHeartRegular,
  DatabasePersonRegular,
  AttachRegular,
  LinkRegular,
  OpenRegular,
  ConnectorRegular,
  CopyRegular,
  EyeRegular,
  CheckRegular,
  StopFilled,
  DocumentTextToolboxRegular,
  BookOpenRegular,
  GlobeRegular,
  ImageStackRegular,
  BrainRegular,
  UsbPlugFilled,
} from "@fluentui/react-icons";

export const iconMap: Record<IconToken, React.ComponentType<any>> = {
  add: AddRegular,
  edit: EditRegular,
  delete: DeleteRegular,
  prompts: PromptRegular,
  resources: DocumentRegular,
  mcpServer: UsbPlugFilled,
  send: SendRegular,
  library: ImageStackRegular,
  connector: ConnectorRegular,
  openLink: OpenRegular,
  robot: BotFilled,
  globe: GlobeRegular,
  brain: BrainRegular,
  bookOpen: BookOpenRegular,
  link: LinkRegular,
  toolResult: DocumentTextToolboxRegular,
  stop: StopFilled,
  menu: Hamburger,
  attachment: AttachRegular,
  completed: CheckRegular,
  eye: EyeRegular,
  databaseGear: DatabasePersonRegular,
  personalization: PersonHeartRegular,
  customize: PeopleSettingsRegular,
  logout: DoorArrowRightRegular,
  copyClipboard: CopyRegular,
  settings: SettingsRegular,
  chevronDown: ChevronDownRegular,
  chevronUp: ChevronUpRegular,
};

export const Button = ({
  variant = "primary",
  size = "medium",
  icon,
  iconPosition = "left",
  children,
  ...rest
}: ComponentProps<"button"> & {
  variant?: string;
  size?: string;
  icon?: IconToken;
  iconPosition?: "left" | "right";
  children?: React.ReactNode;
}): JSX.Element => {
  const IconElem = icon ? iconMap[icon] : undefined;
  return (
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
      icon={IconElem && iconPosition === "left" ? <IconElem /> : undefined}
      iconAfter={
        IconElem && iconPosition === "right" ? <IconElem /> : undefined
      }
      {...(rest as any)}
    >
      {children}
    </FluentButton>
  );
};
