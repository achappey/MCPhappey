import { jsx as _jsx } from "react/jsx-runtime";
import { Button as FluentButton } from "@fluentui/react-components";
export const Button = ({ variant = "primary", size = "medium", ...rest }) => (_jsx(FluentButton, { appearance: variant === "primary"
        ? "primary"
        : variant === "secondary"
            ? "secondary"
            : variant === "outline"
                ? "outline"
                : "transparent", size: size === "sm" || size === "small"
        ? "small"
        : size === "lg" || size === "large"
            ? "large"
            : "medium", ...rest }));
//# sourceMappingURL=Button.js.map