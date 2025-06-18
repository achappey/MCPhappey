import { jsx as _jsx } from "react/jsx-runtime";
import { Badge as FluentBadge } from "@fluentui/react-components";
export const Badge = ({ bg, text, children, }) => (_jsx(FluentBadge, { color: bg == "primary" ? "brand" : bg, children: text ?? children }));
//# sourceMappingURL=Badge.js.map