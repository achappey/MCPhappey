import { jsx as _jsx } from "react/jsx-runtime";
import { Switch as FluentSwitch } from "@fluentui/react-components";
export const Switch = ({ id, label, checked, onChange, className, }) => (_jsx(FluentSwitch, { id: id, checked: checked, onChange: (_, data) => onChange(data.checked), className: className, label: label }));
//# sourceMappingURL=Switch.js.map