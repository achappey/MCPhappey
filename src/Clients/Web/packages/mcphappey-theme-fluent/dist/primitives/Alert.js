import { jsx as _jsx } from "react/jsx-runtime";
import { Alert as FluentAlert } from "@fluentui/react-alert";
export const Alert = ({ variant, className, children, }) => (_jsx(FluentAlert, { appearance: variant === "danger" || variant === "error"
        ? "primary"
        : variant === "warning"
            ? "inverted"
            : undefined, className: className, children: children }));
//# sourceMappingURL=Alert.js.map