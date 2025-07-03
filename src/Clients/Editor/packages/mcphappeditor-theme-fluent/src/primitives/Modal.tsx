import * as React from "react";
import {
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
} from "@fluentui/react-components";

export const Modal = ({
  show,
  onHide,
  size,
  title,
  centered,
  children,
  actions,
}: {
  show: boolean;
  onHide: () => void;
  size?: string;
  title: string;
  centered?: boolean;
  children: React.ReactNode;
  actions?: React.ReactNode;
}): JSX.Element => (
  <Dialog open={show} onOpenChange={(_, data) => !data.open && onHide()}>
    <DialogTrigger disableButtonEnhancement>
      {/* Hidden trigger, modal is controlled */}
      <span style={{ display: "none" }} />
    </DialogTrigger>
    <DialogSurface
      style={
        centered
          ? {
              display: "flex",
              justifyContent: "center",
              alignItems: "center",
            }
          : undefined
      }
    >
      <DialogBody style={{ width: "100%" }}>
        <DialogTitle>{title}</DialogTitle>
        <DialogContent>{children}</DialogContent>
        {actions && <DialogActions>{actions}</DialogActions>}
      </DialogBody>
    </DialogSurface>
  </Dialog>
);
