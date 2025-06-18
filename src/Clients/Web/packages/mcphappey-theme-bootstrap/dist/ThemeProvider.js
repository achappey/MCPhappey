import { jsx as _jsx } from "react/jsx-runtime";
import { ThemeContext } from "mcphappey-core";
import { bootstrapTheme } from "./primitives";
export const ThemeProvider = ({ children }) => (_jsx(ThemeContext.Provider, { value: bootstrapTheme, children: children }));
//# sourceMappingURL=ThemeProvider.js.map