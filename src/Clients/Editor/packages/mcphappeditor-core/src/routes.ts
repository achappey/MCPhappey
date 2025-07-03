import React from "react";
import { RouteObject } from "react-router";
import { ChatPage } from "./components/pages/ChatPage";
import { ServersPage } from "./components/pages/ServersPage";

/**
 * Returns an array of RouteObjects for core pages.
 * @param base Optional base path (e.g. "/")
 */
import { LibraryPage } from "./components/pages/LibraryPage";

export function getCoreRoutes(base = ""): RouteObject[] {
  return [
  /*  {
      path: `${base}chat`,
      element: React.createElement(ChatPage),
    },*/
    {
      path: `${base}servers`,
      element: React.createElement(ServersPage),
    },
    /*{
      path: `${base}library`,
      element: React.createElement(LibraryPage),
    },*/
  ];
}
