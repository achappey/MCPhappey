import { jsx as _jsx } from "react/jsx-runtime";
import { ThemeContext } from "mcphappey-core";
import { fluentTheme } from "./primitives";
import { FluentProvider, webLightTheme } from "@fluentui/react-components";
export const ThemeProvider = ({ children }) => (_jsx(ThemeContext.Provider, { value: fluentTheme, children: _jsx(FluentProvider, { theme: webLightTheme, children: children }) }));
//# sourceMappingURL=ThemeProvider.js.map