import { useTheme } from "../../ThemeContext";
import { useAppStore } from "mcphappeditor-state";
import { useNavigate } from "react-router";

export const ConversationSidebar = () => {
  const { Navigation } = useTheme();
  const sidebarOpen = useAppStore((s) => s.sidebarOpen);
  const setSidebarOpen = useAppStore((s) => s.setSidebarOpen);
  const navigate = useNavigate();
  
  const handleSelect = async (id: string) => {
    if (id === "servers") {
      await navigate("/servers");
    } else {
      await navigate(`/${id}`);
    }
  };
  return (
    <Navigation
      items={[]}
      onClose={() => setSidebarOpen(false)}
      isOpen={sidebarOpen}
      onSelect={handleSelect}
      drawerType="inline"
      style={{ flex: 1, overflowY: "auto", maxHeight: "100%" }}
    />
  );
};
