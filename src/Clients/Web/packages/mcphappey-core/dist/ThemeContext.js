import { createContext, useContext } from "react";
export const ThemeContext = createContext(undefined);
export const useTheme = () => {
    const ctx = useContext(ThemeContext);
    if (!ctx)
        throw new Error("No ThemeProvider found");
    return ctx;
};
//# sourceMappingURL=ThemeContext.js.map