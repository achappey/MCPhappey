import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Card as RBCard, Button as RBButton, Alert as RBAlert, Spinner as RBSpinner, Form, Badge, CloseButton, Modal, Tab, Table, Tabs, } from "react-bootstrap";
export const bootstrapTheme = {
    Button: ({ variant = "primary", size, ...rest }) => (_jsx(RBButton, { variant: variant, size: size, ...rest })),
    Alert: ({ variant, className, children }) => (_jsx(RBAlert, { variant: variant, className: className, children: children })),
    Spinner: ({ size = "sm", className }) => (_jsx(RBSpinner, { animation: "border", size: size, className: className })),
    Modal: (props) => {
        // Only allow "sm" | "lg" | "xl" for size
        const { size, title, children, ...rest } = props;
        const allowed = size === "sm" || size === "lg" || size === "xl" ? size : undefined;
        return (_jsxs(Modal, { size: allowed, ...rest, children: [_jsx(Modal.Header, { closeButton: true, children: _jsx(Modal.Title, { children: title }) }), _jsx(Modal.Body, { children: children })] }));
    },
    Tabs: (props) => {
        // onSelect expects (k: string | null) => void
        const { onSelect, ...rest } = props;
        return (_jsx(Tabs, { ...rest, onSelect: (k) => onSelect &&
                typeof onSelect === "function" &&
                k &&
                onSelect(k) }));
    },
    Tab: (props) => _jsx(Tab, { ...props }),
    Badge: (props) => _jsx(Badge, { ...props }),
    Table: (props) => _jsx(Table, { ...props }),
    CloseButton: (props) => _jsx(CloseButton, { ...props }),
    // Added Switch primitive
    Switch: ({ id, label, checked, onChange, className }) => (_jsx(Form.Check, { type: "switch", id: id, label: label, checked: checked, className: className, onChange: (e) => onChange(e.target.checked) })),
    // Added TextArea primitive
    TextArea: ({ rows, value, onChange, style, className }) => (_jsx(Form.Control, { as: "textarea", rows: rows, value: value, style: style, className: className, onChange: (e) => onChange(e.target.value) })),
    Card: ({ title, text, actions, }) => (_jsx(RBCard, { children: _jsxs(RBCard.Body, { children: [_jsx(RBCard.Title, { children: title }), _jsx(RBCard.Text, { children: text }), actions && _jsx("div", { className: "d-flex gap-2 mt-2", children: actions })] }) })),
    Input: (props) => {
        // Only pass string size ("sm" | "lg") if present, not number
        const { size, value, ...rest } = props;
        const sizeProp = typeof size === "string" && (size === "sm" || size === "lg")
            ? size
            : undefined;
        // Convert readonly string[] to string[] if needed
        let valueProp = value;
        if (Array.isArray(value) && Object.isFrozen(value)) {
            valueProp = Array.from(value);
        }
        // Only pass value if not a readonly array, to avoid TS error
        if (Array.isArray(valueProp) && Object.isFrozen(valueProp)) {
            return _jsx(Form.Control, { ...rest, size: sizeProp });
        }
        return (_jsx(Form.Control, { ...rest, size: sizeProp, value: valueProp }));
    },
};
//# sourceMappingURL=primitives.js.map