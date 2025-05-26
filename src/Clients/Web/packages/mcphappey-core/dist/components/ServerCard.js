import { jsx as _jsx, Fragment as _Fragment, jsxs as _jsxs } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
import { useMcpStore } from "mcphappey-state";
import { useMcpConnect } from "../hooks/useMcpConnect";
const ServerCard = ({ server, onShowDetails }) => {
    const { Button, Card } = useTheme();
    const { connect } = useMcpConnect();
    const { disconnect, isConnected } = useMcpStore();
    const connected = isConnected(server.url);
    return (_jsx(Card, { title: server.name, text: server.url, actions: _jsxs(_Fragment, { children: [_jsx(Button, { onClick: () => connected ? disconnect(server.url) : connect(server.url), variant: connected ? "success" : "primary", size: "sm", disabled: false, children: connected ? "Disconnect" : "Connect" }), connected && (_jsx(Button, { onClick: () => onShowDetails(server), variant: "secondary", size: "sm", children: "Details" }))] }) }));
};
export default ServerCard;
//# sourceMappingURL=ServerCard.js.map