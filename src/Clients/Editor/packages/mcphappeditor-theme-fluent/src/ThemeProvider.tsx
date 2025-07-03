import { ThemeContext } from "mcphappeditor-core";
import { fluentTheme } from "./primitives";
import {
  FluentProvider,
  webLightTheme,
  webDarkTheme,
} from "@fluentui/react-components";
import { useDarkMode } from "usehooks-ts";

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
  const { isDarkMode } = useDarkMode();

  return (
    <ThemeContext.Provider value={fluentTheme}>
      <FluentProvider theme={isDarkMode ? webDarkTheme : webLightTheme}>
        {children}
      </FluentProvider>
    </ThemeContext.Provider>
  );
};
