import { jsx as _jsx } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
const PromptCard = ({ prompt, onOpen }) => {
    const { Card, Button } = useTheme();
    return (_jsx(Card, { title: prompt.name, text: prompt.description ?? "", actions: _jsx(Button, { variant: "primary", size: "sm", onClick: () => onOpen(prompt), children: "Open" }) }));
};
export default PromptCard;
//# sourceMappingURL=PromptCard.js.map