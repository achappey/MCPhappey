import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
const ResourceCard = ({ resource, onRead, onOpenLink }) => {
    const { Card, Button } = useTheme();
    return (_jsx(Card, { title: resource.name, text: resource.description ?? "", actions: _jsxs("div", { style: { display: "flex", gap: 8 }, children: [_jsx(Button, { variant: "primary", size: "sm", onClick: () => onRead(resource), children: "Read resource" }), _jsx(Button, { variant: "secondary", size: "sm", onClick: () => onOpenLink(resource), children: "Open link" })] }) }));
};
export default ResourceCard;
//# sourceMappingURL=ResourceCard.js.map