import { jsx as _jsx } from "react/jsx-runtime";
import { Spinner as FluentSpinner } from "@fluentui/react-components";
export const Spinner = ({ size = "tiny", className, }) => (_jsx(FluentSpinner, { size: size === "sm" || size === "tiny"
        ? "tiny"
        : size === "xs" || size === "extra-small"
            ? "extra-small"
            : size === "md" || size === "medium"
                ? "medium"
                : size === "lg" || size === "large"
                    ? "large"
                    : "tiny", className: className }));
//# sourceMappingURL=Spinner.js.map