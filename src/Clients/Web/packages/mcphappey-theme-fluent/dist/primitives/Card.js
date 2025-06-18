import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Card as FluentCard, CardHeader, CardFooter, CardPreview } from "@fluentui/react-components";
export const Card = ({ title, text, actions, }) => (_jsxs(FluentCard, { children: [_jsx(CardHeader, { header: _jsx("span", { children: title }) }), _jsx(CardPreview, { children: text }), actions && _jsx(CardFooter, { children: actions })] }));
//# sourceMappingURL=Card.js.map