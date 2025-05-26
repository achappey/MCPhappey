import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Dialog, DialogTrigger, DialogSurface, DialogBody, DialogTitle, DialogContent, } from "@fluentui/react-components";
export const Modal = ({ show, onHide, size, title, centered, children, }) => (_jsxs(Dialog, { open: show, onOpenChange: (_, data) => !data.open && onHide(), children: [_jsx(DialogTrigger, { disableButtonEnhancement: true, children: _jsx("span", { style: { display: "none" } }) }), _jsx(DialogSurface, { style: centered
                ? {
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center",
                }
                : undefined, children: _jsxs(DialogBody, { style: { width: "100%" }, children: [_jsx(DialogTitle, { children: title }), _jsx(DialogContent, { children: children })] }) })] }));
//# sourceMappingURL=Modal.js.map