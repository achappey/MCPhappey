
// esbuild.config.js

const esbuild = require("esbuild");
const isWatch = process.argv.includes("--watch");

// Laad .env of .env.production afhankelijk van NODE_ENV
require("dotenv").config({
  path: `.env${process.env.NODE_ENV === "production" ? ".production" : ""}`
});

// --- Environment variabelen inlezen met fallbacks ---
function safeParseJSON(str, fallback) {
  try {
    return JSON.parse(str);
  } catch {
    return fallback;
  }
}

const mcpEditApi      = process.env.MCP_EDIT_API_URL            || "http://localhost:3010/api";
const appName         = process.env.APP_NAME                || "YACB";

// Deze kunnen JSON of string zijn, dus altijd even JSON.stringify voor define
const msalClientId   = process.env.MSAL_CLIENT_ID           || "<client_id>";
const msalAuthority  = process.env.MSAL_AUTHORITY           || "https://login.microsoftonline.com/<tenant_id>";
const msalRedirectUri= process.env.MSAL_REDIRECT_URI        || "/";
const msalScopes     = safeParseJSON(process.env.MSAL_SCOPES, ["<scope>"]);

// --- App version/tag op buildtijd (YYYYMMDD.HHmm) ---
const now = new Date();
const pad = (n) => n.toString().padStart(2, "0");
const buildDateVersion =
  now.getFullYear().toString() +
  pad(now.getMonth() + 1) +
  pad(now.getDate()) + "." +
  pad(now.getHours()) +
  pad(now.getMinutes());
// --- Esbuild opties ---
const buildOptions = {
  entryPoints: ["src/main.tsx"],
  bundle: true,
  outfile: "public/bundle.js",
  sourcemap: true,
  minify: !isWatch, // Alleen minify bij production build
  define: {
    "process.env.NODE_ENV": isWatch ? '"development"' : '"production"',
    "__MCP_EDIT_API__": JSON.stringify(mcpEditApi),
    "__APP_VERSION__": JSON.stringify(`${buildDateVersion}.fluent`),
    "__APP_NAME__": JSON.stringify(appName),
    "__MSAL_CLIENT_ID__": JSON.stringify(msalClientId),
    "__MSAL_AUTHORITY__": JSON.stringify(msalAuthority),
    "__MSAL_REDIRECT_URI__": JSON.stringify(msalRedirectUri),
    "__MSAL_SCOPES__": JSON.stringify(msalScopes),
  },
  loader: { ".tsx": "tsx", ".ts": "ts" },
};

// --- Build of watch ---
if (isWatch) {
  esbuild.context(buildOptions)
    .then(ctx => {
      ctx.watch();
      console.log("Watching for changes...");
    })
    .catch((err) => {
      console.error("Build failed:", err);
      process.exit(1);
    });
} else {
  esbuild.build(buildOptions)
    .then(() => {
      console.log("Build complete.");
    })
    .catch((err) => {
      console.error("Build failed:", err);
      process.exit(1);
    });
}
