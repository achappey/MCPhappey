import { jsx as _jsx } from "react/jsx-runtime";
import { Button as FluentButton } from "@fluentui/react-components";
import { Dismiss24Regular } from "@fluentui/react-icons";
export const CloseButton = ({ onClick, className, style, "aria-label": ariaLabel, }) => (_jsx(FluentButton, { appearance: "subtle", shape: "circular", icon: _jsx(Dismiss24Regular, {}), "aria-label": ariaLabel || "Close", onClick: onClick, className: className, style: style }));
//# sourceMappingURL=CloseButton.js.map