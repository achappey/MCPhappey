import type { ComponentProps, ComponentType, JSX, ReactNode } from "react";
import type { ChatMessage } from "./chat";
import { UserMenuLabels } from "./i18n";

// Semantic icon token, not tied to any icon library
export type IconToken =
  | 'add'
  | 'edit'
  | 'delete'
  | 'send'
  | 'robot'
  | 'customize'
  | 'mcpServer'
  | 'prompts'
  | 'eye'
  | 'completed'
  | 'brain'
  | 'databaseGear'
  | 'openLink'
  | "attachment"
  | 'stop'
  | 'resources'
  | 'library'
  | 'menu'
  | 'globe'
  | 'bookOpen'
  | 'toolResult'
  | 'copyClipboard'
  | 'connector'
  | 'link'
  | 'personalization'
  | 'settings'
  | 'chevronDown'
  | 'chevronUp'
  | 'logout';

// Drawer/Offcanvas cross-framework types
export type DrawerPosition = 'start' | 'end' | 'top' | 'bottom';
export type DrawerSize = 'small' | 'medium' | 'large' | 'full';

/**
 * Generic DataGrid type for theme injection
 */
export type DataGridColumn<T> = {
  id: string;
  header: React.ReactNode;
  render: (item: T) => React.ReactNode;
  sortable?: boolean;
  width?: number;
};

export interface DataGridProps<T> {
  items: T[];
  columns: DataGridColumn<T>[];
  keySelector: (item: T) => React.Key;
  selectable?: "single" | "multi";
  sortable?: boolean;
  onSelectionChange?: (keys: React.Key[]) => void;
  onSortChange?: (colId: string, dir: "asc" | "desc") => void;
  style?: React.CSSProperties;
  nativeProps?: Record<string, any>;
}

export interface DrawerProps {
  open: boolean;
  onClose: () => void;
  title?: React.ReactNode;
  children: React.ReactNode;
  overlay?: boolean
  position?: DrawerPosition; // default 'end'
  size?: DrawerSize;         // default 'small'
  backdrop?: boolean;        // default true
}

export interface NavigationItem {
  key: string;
  label: string | React.ReactNode;
  icon?: IconToken;
  href?: string;
  disabled?: boolean;
  conversationItem?: boolean;
  onClick?: any
  eventKey?: string;
  [key: string]: any;
}

export interface TagGroup {
  items?: TagItem[]
  onRemove?: (id: any) => Promise<void>
}

export interface TagItem {
  key: string;
  label: string | React.ReactNode;
}

export interface NavigationProps {
  items: NavigationItem[];
  activeKey?: string;
  onClose?: () => void;
  onSelect?: (key: string) => void;
  isOpen?: boolean
  onNewChat?: () => void;
  storageType?: "local" | "remote";
  onStorageSwitch?: (newType: "local" | "remote") => void;
  multiple?: boolean;
  drawerType?: "inline" | "overlay";
  className?: string;
  onDelete?: (id: string) => Promise<void>
  onRename?: (id: string, newName: string) => Promise<void>
  style?: React.CSSProperties;
}

export type MenuItemProps = {
  key: string;
  label: string;
  onClick: () => void | Promise<void>;
  danger?: boolean; // Optional for "delete" style
};

export type MenuProps = {
  items: MenuItemProps[];
  trigger?: React.ReactElement; // Optional, defaults to "More"
  align?: "left" | "right"; // Optional
  size?: "small" | "medium"; // Optional
  className?: string;
};


export interface AihUiTheme {
   DataGrid: <T>(props: DataGridProps<T>) => JSX.Element;
  /**
   * Typography heading (h1-h6)
   */
  Header: (props: {
    /** 1–6 ⇒ <h1> … <h6>  (default = 1) */
    level?: 1 | 2 | 3 | 4 | 5 | 6;
    className?: string;
    children: React.ReactNode;
    style?: React.CSSProperties;
  }) => JSX.Element;

  /**
   * Typography paragraph/body text
   */
  Paragraph: (props: {
    className?: string;
    children: React.ReactNode;
    style?: React.CSSProperties;
  }) => JSX.Element;
  /**
     * Breadcrumb navigation primitive
     */
  Breadcrumb: (props: {
    items: {
      key: string;
      icon?: IconToken;
      label: React.ReactNode;
      onClick?: () => void
    }[];
    separator?: React.ReactNode;
    className?: string;

    size?: "small" | "medium" | "large" | undefined;
    style?: React.CSSProperties;
  }) => JSX.Element;

  Button: (props: ComponentProps<"button"> & {
    variant?: string;
    size?: string;
    icon?: IconToken;
    iconPosition?: 'left' | 'right';
  }) => JSX.Element;

  UserMenu: ComponentType<{
    email?: string;
    onCustomize: () => void;
    onSettings: () => void;
    onLogout: () => void;
    className?: string;
    labels?: UserMenuLabels
    style?: React.CSSProperties;
  }>;
  Input: (props: ComponentProps<"input"> & {
    label?: string;
    hint?: string;
  }) => JSX.Element;
  Image: (props: {
    fit?: "none" | "center" | "contain" | "cover" | "default";
    shadow?: boolean;
    block?: boolean;
    src?: string;
    bordered?: boolean;
    shape?: 'circular' | 'rounded' | 'square';
  }) => JSX.Element;
  Card: (props: {
    title: string;
    size?: "small" | "medium" | "large" | undefined,
    text?: string;
    description?: string;
    actions?: JSX.Element,
    headerActions?: JSX.Element,
    style?: React.CSSProperties;
    children?: React.ReactNode
  }) => JSX.Element;
  Alert: (props: {
    variant: string;
    onDismiss?: () => void,
    title?: string,
    className?: string;
    children: React.ReactNode
  }) => JSX.Element;
  Spinner: (props: { size?: string; label?: string; className?: string }) => JSX.Element;
  Modal: (props: {
    title: string,
    show: boolean; onHide: () => void; size?: string;
    centered?: boolean; children: React.ReactNode;
    actions?: React.ReactNode
  }) => JSX.Element;
  Tabs: (props: {
    style?: React.CSSProperties,
    activeKey: string; vertical?: boolean,
    onSelect: (k: string) => void; className?: string; children: React.ReactNode
  }) => JSX.Element;
  Tab: (props: {
    eventKey: string; icon?: IconToken,
    title: React.ReactNode; children: React.ReactNode
  }) => JSX.Element;
  Badge: (props: {
    bg?: string; text?: string;
    children: React.ReactNode
  }) => JSX.Element;
  Table: (props: {
    hover?: boolean, bordered?: boolean,
    striped?: boolean, borderless?: boolean;
    size?: string; children: React.ReactNode
  }) => JSX.Element;
  CloseButton: (props: {
    onClick: (e: any) => void;
    className?: string; style?: React.CSSProperties; "aria-label"?: string
  }) => JSX.Element;
  Select: ComponentType<any>;
  SearchBox: (props: {
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
    disabled?: boolean;
    style?: React.CSSProperties;
    className?: string;
    autoFocus?: boolean;
  }) => JSX.Element;
  ProgressBar: (props: {
    value?: number; // 0-100
    label?: string;
    variant?: string;
    striped?: boolean;
    animated?: boolean;
    className?: string;
  }) => JSX.Element;

  // Added for UI framework agnostic toggles and textareas
  Switch: (props: {
    id: string;
    label?: string;
    hint?: string;
    required?: boolean;
    checked: boolean;
    onChange: (checked: boolean) => void;
    className?: string;
  }) => JSX.Element;

  TextArea: (props: {
    ref?: any;
    onKeyDown?: any
    rows?: number;
    hint?: string;
    placeholder?: string
    label?: string
    value: string;
    readOnly?: boolean
    onChange?: (value: string) => void;
    style?: React.CSSProperties;
    className?: string;
  }) => React.ReactNode;

  Toolbar: (props: {
    ariaLabel?: string;
    size?: "small" | "medium" | "large";
    className?: string;
    children: React.ReactNode;
  }) => JSX.Element;

  Chat: (props: {
    onShowSources?: any, onShowTools?: any,
    messages?: ChatMessage[]
  }) => JSX.Element;
  Drawer: (props: DrawerProps) => JSX.Element | null;
  Navigation: (props: NavigationProps) => JSX.Element;
  Menu: (props: MenuProps) => JSX.Element;
  Tags: (props: TagGroup) => JSX.Element;
  /**
   * Toast notification primitive for status/info/success/error.
   * @param id unique key for updating/dismissing
   * @param variant info | success | error
   * @param message main content (ReactNode)
   * @param show whether toast is visible
   * @param autohide ms to auto-dismiss (undefined = manual)
   * @param onClose callback for manual close
   */
  Toast: (props: {
    id: string;
    variant: 'info' | 'success' | 'error';
    message: React.ReactNode;
    show: boolean;
    autohide?: number;
    onClose?: () => void;
  }) => JSX.Element;
  /**
   * Skeleton placeholder for loading content.
   * @param width width of the skeleton (number or string)
   * @param height height of the skeleton (number or string)
   * @param circle render as a circle
   * @param animation animation style: 'pulse' | 'wave'
   * @param className optional className
   */
  Skeleton: (props: {
    width?: number | string;
    height?: number | string;
    circle?: boolean;
    animation?: 'pulse' | 'wave';
    className?: string;
    style?: React.CSSProperties;
  }) => JSX.Element;
  /**
   * Carousel/slider primitive for displaying a sequence of slides.
   * @param id Optional unique id for the carousel
   * @param activeIndex Controlled active slide index (zero-based)
   * @param onSelect Callback when slide changes
   * @param interval Auto-advance interval in ms (0 = none)
   * @param controls Show next/prev controls (default: true)
   * @param indicators Show slide indicators (default: true)
   * @param slides Array of slides (key, content, optional caption)
   * @param className Optional className
   * @param style Optional style
   */
  Carousel: (props: {
    id?: string;
    activeIndex?: number;
    onSelect?: (newIndex: number) => void;
    interval?: number;
    controls?: boolean;
    indicators?: boolean;
    slides: Array<{
      key: string;
      content: React.ReactNode;
      caption?: React.ReactNode;
    }>;
    className?: string;
    style?: React.CSSProperties;
  }) => JSX.Element;

}
