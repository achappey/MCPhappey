import { ThemeContext } from "mcphappey-core";
import { fluentTheme } from "./primitives";
import { FluentProvider, webLightTheme } from "@fluentui/react-components";

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => (
  <ThemeContext.Provider value={fluentTheme}>
    <FluentProvider theme={webLightTheme}>{children}</FluentProvider>
  </ThemeContext.Provider>
);
