import { ThemeContext } from "mcphappeditor-core";
import { bootstrapTheme } from "./primitives";

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => (
  <ThemeContext.Provider value={bootstrapTheme}>
    {children}
  </ThemeContext.Provider>
);
