import { jsx as _jsx } from "react/jsx-runtime";
import { Textarea as FluentTextarea } from "@fluentui/react-components";
export const TextArea = ({ rows, value, onChange, style, className, }) => (_jsx(FluentTextarea, { rows: rows, value: value, onChange: (_, data) => onChange(data.value), style: style, className: className }));
//# sourceMappingURL=TextArea.js.map