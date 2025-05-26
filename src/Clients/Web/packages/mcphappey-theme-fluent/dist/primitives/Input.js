import { jsx as _jsx } from "react/jsx-runtime";
import { Input as FluentInput } from "@fluentui/react-components";
export const Input = (props) => {
    const { size, ...rest } = props;
    const sizeProp = typeof size === "string"
        ? size === "sm" || size === "small"
            ? "small"
            : size === "lg" || size === "large"
                ? "large"
                : "medium"
        : "medium";
    return _jsx(FluentInput, { size: sizeProp, ...rest });
};
//# sourceMappingURL=Input.js.map