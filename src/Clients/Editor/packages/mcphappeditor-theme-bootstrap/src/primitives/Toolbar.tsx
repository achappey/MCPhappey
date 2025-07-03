import * as React from "react";
import ButtonToolbar from "react-bootstrap/ButtonToolbar";

export const Toolbar: React.FC<{
  ariaLabel?: string;
  className?: string;
  children: React.ReactNode;
}> = ({ ariaLabel, className, children, ...rest }) => (
  <ButtonToolbar aria-label={ariaLabel} className={className} {...rest}>
    {children}
  </ButtonToolbar>
);
