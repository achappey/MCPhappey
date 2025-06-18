import { jsx as _jsx } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
const ToolCard = ({ tool, onOpen }) => {
    const { Card, Button } = useTheme();
    return (_jsx(Card, { title: tool.name, text: tool.description ?? "", actions: _jsx(Button, { variant: "primary", size: "sm", onClick: () => onOpen(tool), children: "Open" }) }));
};
export default ToolCard;
//# sourceMappingURL=ToolCard.js.map