import { useEffect, useState } from "react";
import { useAppStore } from "mcphappeditor-state";
import { useTheme } from "../../ThemeContext";

type ToastPhase = "info" | "success" | "error";
type ToastState = {
  id: string;
  url: string;
  phase: ToastPhase;
  message: string;
  show: boolean;
  autohide?: number;
};

export const ToastProvider = ({ children }: { children: React.ReactNode }) => {
  const status = useAppStore((s) => s.status);
  const theme = useTheme();
  const [toasts, setToasts] = useState<ToastState[]>([]);

  // Helper functions to manipulate toasts array
  const showToast = (toast: ToastState) => {
    setToasts((prev) => {
      const idx = prev.findIndex((t) => t.url === toast.url);

      // If toast for this URL already exists …
      if (idx >= 0) {
        const prevToast = prev[idx];

        // …and its phase changed (e.g. "connecting" → "connected"),
        // create a **new** toast with a fresh id so theme primitives
        // (especially Fluent) treat it as a brand-new notification.
        if (prevToast.phase !== toast.phase) {
          const newToast: ToastState = {
            ...toast,
            id: `${toast.id}-${toast.phase}-${Date.now()}`,
            show: true,
          };
          return [...prev.slice(0, idx), newToast, ...prev.slice(idx + 1)];
        }

        // Otherwise update the existing toast in place
        const updated = { ...prevToast, ...toast, show: true };
        return [...prev.slice(0, idx), updated, ...prev.slice(idx + 1)];
      }

      // First time for this URL – just add it
      return [...prev, toast];
    });
  };

  const hideToast = (url: string) => {
    setToasts((prev) =>
      prev.map((t) => (t.url === url ? { ...t, show: false } : t))
    );
  };

  const removeToast = (url: string) => {
    setToasts((prev) => prev.filter((t) => t.url !== url));
  };

  // Watch status map and update toasts
  useEffect(() => {
    Object.entries(status).forEach(([url, st]) => {
      if (st === "connecting") {
        console.log(st)
        showToast({
          id: url,
          url,
          phase: "info",
          message: "Connecting to server...",
          show: true,
        });
      } else if (st === "connected") {
        console.log(st)
        showToast({
          id: url,
          url,
          phase: "success",
          message: "Connected",
          show: true,
          autohide: 3000,
        });
        //setTimeout(() => hideToast(url), 3000);
        //setTimeout(() => removeToast(url), 3500);
      } else if (st === "error") {
        showToast({
          id: url,
          url,
          phase: "error",
          message: "Connection failed",
          show: true,
        });
      } else if (st === "idle") {
        removeToast(url);
      }
    });
    // Remove toasts for servers no longer in status
    setToasts((prev) => prev.filter((t) => status[t.url]));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [JSON.stringify(status)]);

  return (
    <>
      {toasts.map((toast) => (
        <theme.Toast
          key={toast.id}
          id={toast.id}
          variant={toast.phase}
          message={toast.message}
          show={toast.show}
          autohide={toast.autohide}
          onClose={() => removeToast(toast.url)}
        />
      ))}

      {children}
    </>
  );
};
