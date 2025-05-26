import { createContext, useContext } from "react";
import type { McphUiTheme } from "mcphappey-types";

export const ThemeContext = createContext<McphUiTheme | undefined>(undefined);

export const useTheme = (): McphUiTheme => {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("No ThemeProvider found");
  return ctx;
};
