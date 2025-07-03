import { Outlet } from "react-router";
import { ConversationSidebar } from "../chat/ConversationSidebar";
import { MinimalNavBar } from "../navigation/MinimalNavBar";
import { useAppStore } from "mcphappeditor-state";

export const SidebarLayout = () => {
  const sidebarOpen = useAppStore((s) => s.sidebarOpen);

  return (
    <div
      style={{
        display: "flex",
        height: "100dvh",
        minWidth: 0,
      }}
    >
      {!sidebarOpen && <MinimalNavBar />}
      {sidebarOpen && (
        <div
          style={{
            height: "100%",
            display: "flex",
            flexDirection: "column",
          }}
        >
          <ConversationSidebar />
        </div>
      )}
      <div
        style={{
          flex: 1,
          minWidth: 0,
          display: "flex",
          overflowY: "auto",
          flexDirection: "column",
        }}
      >
        <Outlet />
      </div>
    </div>
  );
};
