import React from "react";
import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";

interface UserMenuButtonProps {
  email?: string;
  onCustomize: () => void;
  onSettings: () => void;
  onLogout: () => void;
  className?: string;
  style?: React.CSSProperties;
}

export const UserMenuButton: React.FC<UserMenuButtonProps> = ({
  email,
  onCustomize,
  onSettings,
  onLogout,
  className,
  style,
}) => {
  const { UserMenu } = useTheme();
  const { t } = useTranslation();
  return (
    <UserMenu
      email={email}
      onCustomize={onCustomize}
      onSettings={onSettings}
      onLogout={onLogout}
      labels={{
        customize: t("userMenu.personalization"),
        settings: t("userMenu.settings"),
        logout: t("userMenu.logout"),
      }}
      className={className}
      style={style}
    />
  );
};
