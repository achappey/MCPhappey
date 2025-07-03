import {
  Placeholder as RBPlaceholder,
  Card as RBCard,
  Button as RBButton,
  Alert as RBAlert,
  Spinner as RBSpinner,
  ProgressBar as RBProgressBar,
  Form,
  Badge,
  CloseButton,
  Modal,
  Tab,
  Table,
  Tabs,
} from "react-bootstrap";
import type { ComponentProps, JSX } from "react";
import type { AihUiTheme, IconToken } from "mcphappeditor-types";
import { Chat } from "./primitives/Chat";
import { Select } from "./primitives/Select";
import { Drawer } from "./primitives/Drawer";
import { Image } from "./primitives/Image";
import { Carousel } from "./primitives/Carousel";
import {
  Plus,
  Pencil,
  Trash,
  Send,
  Gear,
  ChevronDown,
  ChevronUp,
  File,
  Plug,
  Chat as BSChatIcon,
  Robot,
  Sliders,
  PersonHeart,
  DatabaseGear,
  Plugin,
  Paperclip,
  Link,
  BoxArrowUpRight,
  Copy,
  List,
  Eye,
  Check,
  StopFill,
  UiChecks,
  Globe,
  JournalText,
  Images,
  Cpu,
  CpuFill,
} from "react-bootstrap-icons";
import React from "react";

import { UserMenu } from "./primitives/UserMenu";
import { Toolbar } from "./primitives/Toolbar";
import { Navigation } from "./primitives/Navigation";
import Tags from "./primitives/Tags";
import { SearchBox } from "./primitives/SearchBox";
import { Menu } from "./primitives/Menu";
import { Toast } from "./primitives/Toast";
import { Breadcrumb } from "./primitives/Breadcrumb";
import * as primitives from "./primitives";
import { DataGrid } from "./primitives/DataGrid";


// Typography primitives
const Header = ({
  level = 1,
  className,
  children,
}: {
  level?: 1 | 2 | 3 | 4 | 5 | 6;
  className?: string;
  children: React.ReactNode;
}) => {
  const Tag = `h${level}` as keyof JSX.IntrinsicElements;
  return <Tag className={className}>{children}</Tag>;
};

const Paragraph = ({
  className,
  children,
}: {
  className?: string;
  children: React.ReactNode;
}) => <p className={className}>{children}</p>;

const iconMap: Record<IconToken, JSX.Element> = {
  add: <Plus />,
  edit: <Pencil />,
  customize: <Sliders />,
  copyClipboard: <Copy />,
  delete: <Trash />,
  prompts: <BSChatIcon />,
  globe: <Globe />,
  brain: <CpuFill />,
  bookOpen: <JournalText />,
  library: <Images />,
  link: <Link />,
  completed: <Check />,
  attachment: <Paperclip />,
  connector: <Plugin />,
  stop: <StopFill />,
  openLink: <BoxArrowUpRight />,
  toolResult: <UiChecks />,
  resources: <File />,
  eye: <Eye />,
  menu: <List />,
  databaseGear: <DatabaseGear />,
  personalization: <PersonHeart />,
  mcpServer: <Plug />,
  send: <Send />,
  settings: <Gear />,
  robot: <Robot />,
  chevronDown: <ChevronDown />,
  chevronUp: <ChevronUp />,
  logout: <Trash />, // You may want to use a better icon, e.g. BoxArrowRight, but Trash is a placeholder
};


export const bootstrapTheme: AihUiTheme = {
  Header,
  DataGrid,
  Paragraph,
  Breadcrumb,
  Skeleton: ({
    width,
    height,
    circle,
    animation = "pulse",
    className,
  }: {
    width?: number | string;
    height?: number | string;
    circle?: boolean;
    animation?: "pulse" | "wave";
    className?: string;
  }) => (
    <RBPlaceholder
      as="span"
      animation={animation === "wave" ? "wave" : "glow"}
      className={className}
      style={{
        width,
        height,
        borderRadius: circle ? "50%" : undefined,
        display: "inline-block",
      }}
    />
  ),
  Toast,
  Button: ({
    variant = "primary",
    size,
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
  }): JSX.Element => (
    <RBButton variant={variant} size={size as any} {...(rest as any)}>
      {icon && iconPosition === "left" && (
        <span className="me-2">{iconMap[icon]}</span>
      )}
      {children}
      {icon && iconPosition === "right" && (
        <span className="ms-2">{iconMap[icon]}</span>
      )}
    </RBButton>
  ),
  UserMenu,
  Select,
  Navigation: Navigation as any,
  SearchBox: SearchBox as any,
  Tags: Tags as any,
  Menu: Menu as any,
  Image,
  Alert: ({ variant, className, children }): JSX.Element => (
    <RBAlert variant={variant as any} className={className}>
      {children}
    </RBAlert>
  ),

  Spinner: ({ size = "sm", className }): JSX.Element => (
    <RBSpinner animation="border" size={size as any} className={className} />
  ),

  ProgressBar: ({
    value = 0,
    label,
    variant,
    striped,
    animated,
    className,
  }: {
    value?: number;
    label?: string;
    variant?: string;
    striped?: boolean;
    animated?: boolean;
    className?: string;
  }) => (
    <RBProgressBar
      now={value}
      label={label}
      variant={variant as any}
      striped={striped}
      animated={animated}
      className={className}
    />
  ),
  Modal: (props) => {
    // Only allow "sm" | "lg" | "xl" for size
    const { size, title, children, actions, ...rest } = props;
    const allowed =
      size === "sm" || size === "lg" || size === "xl" ? size : undefined;
    return (
      <Modal size={allowed} {...rest}>
        <Modal.Header closeButton>
          <Modal.Title>{title}</Modal.Title>
        </Modal.Header>
        <Modal.Body>{children}</Modal.Body>
        {actions && <Modal.Footer>{actions}</Modal.Footer>}
      </Modal>
    );
  },
  Tabs: (props) => {
    // onSelect expects (k: string | null) => void
    const { onSelect, ...rest } = props;
    return (
      <Tabs
        {...rest}
        onSelect={(k) =>
          onSelect &&
          typeof onSelect === "function" &&
          k &&
          onSelect(k as string)
        }
      />
    );
  },
  Tab: (props) => <Tab {...props} />,
  Badge: (props) => <Badge {...props} />,
  Table: (props) => <Table {...props} />,
  CloseButton: (props) => <CloseButton {...props} />,

  Chat,

  // Added Switch primitive
  Switch: ({ id, label, checked, onChange, className }) => (
    <Form.Check
      type="switch"
      id={id}
      label={label}
      checked={checked}
      className={className}
      onChange={(e) => onChange(e.target.checked)}
    />
  ),

  // Added TextArea primitive
  TextArea: ({ rows, readOnly, value, onChange, style, className }) => (
    <Form.Control
      as="textarea"
      rows={rows}
      disabled={readOnly}
      value={value}
      style={style}
      className={className}
      onChange={(e) => onChange && onChange(e.target.value)}
    />
  ),

  Card: ({
    title,
    text,
    children,
    actions,
  }: {
    title: string;
    text?: string;
    children?: React.ReactNode;
    actions?: JSX.Element;
  }): JSX.Element => (
    <RBCard>
      <RBCard.Body>
        <RBCard.Title>{title}</RBCard.Title>
        <RBCard.Text>{children ?? text}</RBCard.Text>
        {actions && <div className="d-flex gap-2 mt-2">{actions}</div>}
      </RBCard.Body>
    </RBCard>
  ),

  Drawer,
  Toolbar: Toolbar as any,

  Input: (props: ComponentProps<"input">): JSX.Element => {
    // Only pass string size ("sm" | "lg") if present, not number
    const { size, value, ...rest } = props;
    const sizeProp =
      typeof size === "string" && (size === "sm" || size === "lg")
        ? size
        : undefined;
    // Convert readonly string[] to string[] if needed
    let valueProp = value;
    if (Array.isArray(value) && Object.isFrozen(value)) {
      valueProp = Array.from(value);
    }
    // Only pass value if not a readonly array, to avoid TS error
    if (Array.isArray(valueProp) && Object.isFrozen(valueProp)) {
      return <Form.Control {...(rest as any)} size={sizeProp} />;
    }
    return (
      <Form.Control
        {...(rest as any)}
        size={sizeProp}
        value={valueProp as string | number | string[] | undefined}
      />
    );
  },
  Carousel,
};

