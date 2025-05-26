import type { ComponentProps, JSX } from "react";

export interface McphUiTheme {
  Button: (props: ComponentProps<"button"> & { variant?: string; size?: string }) => JSX.Element;
  Input: (props: ComponentProps<"input">) => JSX.Element;
  Card: (props: { title: string; text: string; actions?: JSX.Element }) => JSX.Element;
  Alert: (props: { variant: string; className?: string; children: React.ReactNode }) => JSX.Element;
  Spinner: (props: { size?: string; className?: string }) => JSX.Element;
  Modal: (props: { title: string, show: boolean; onHide: () => void; size?: string; centered?: boolean; children: React.ReactNode }) => JSX.Element;
  Tabs: (props: { activeKey: string; onSelect: (k: string) => void; className?: string; children: React.ReactNode }) => JSX.Element;
  Tab: (props: { eventKey: string; title: React.ReactNode; children: React.ReactNode }) => JSX.Element;
  Badge: (props: { bg?: string; text?: string; children: React.ReactNode }) => JSX.Element;
  Table: (props: { hover?: boolean, bordered?: boolean, striped?: boolean, borderless?: boolean; size?: string; children: React.ReactNode }) => JSX.Element;
  CloseButton: (props: { onClick: (e: any) => void; className?: string; style?: React.CSSProperties; "aria-label"?: string }) => JSX.Element;

  // Added for UI framework agnostic toggles and textareas
  Switch: (props: {
    id: string;
    label?: string;
    checked: boolean;
    onChange: (checked: boolean) => void;
    className?: string;
  }) => JSX.Element;

  TextArea: (props: {
    rows?: number;
    value: string;
    onChange: (value: string) => void;
    style?: React.CSSProperties;
    className?: string;
  }) => JSX.Element;
}
