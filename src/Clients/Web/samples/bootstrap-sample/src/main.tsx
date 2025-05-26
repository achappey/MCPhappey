import React from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter, RouterProvider } from "react-router";
import App from "./App";
import { OAuthCallbackPage } from "mcphappey-auth";

const router = createBrowserRouter([
  {
    path: "/oauth-callback",
    element: <OAuthCallbackPage />,
  },
  {
    path: "/*",
    element: <App />,
  },
]);
const container = document.getElementById("root");

if (container) {
  const root = createRoot(container);
  root.render(<RouterProvider router={router} />);
}
