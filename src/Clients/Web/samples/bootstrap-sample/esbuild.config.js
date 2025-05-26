const esbuild = require("esbuild");

const isWatch = process.argv.includes("--watch");

// Expect env var to be a JSON string array, e.g., '["url1", "url2"]'
const defaultUrlsEnv = process.env.DEFAULT_MCP_SERVER_LIST_URLS_JSON;
let defaultUrls = ["http://localhost:3001/mcp.json"]; // Default fallback array

if (defaultUrlsEnv) {
  try {
    const parsed = JSON.parse(defaultUrlsEnv);
    if (Array.isArray(parsed) && parsed.every(item => typeof item === 'string')) {
      defaultUrls = parsed;
    } else {
      console.warn("Invalid format for DEFAULT_MCP_SERVER_LIST_URLS_JSON env var. Using default.");
    }
  } catch (e) {
    console.warn("Error parsing DEFAULT_MCP_SERVER_LIST_URLS_JSON env var. Using default.", e);
  }
}

const buildOptions = {
  entryPoints: ["src/main.tsx"],
  bundle: true,
  outfile: "public/bundle.js",
  sourcemap: true,
  minify: false, // Keep false for easier debugging if needed
  define: {
    "process.env.NODE_ENV": isWatch ? '"development"' : '"production"', // Adjust based on watch/build
    // Define the new constant, ensuring the value is a stringified array
    "__DEFAULT_MCP_SERVER_LIST_URLS__": JSON.stringify(defaultUrls),
  },
  loader: { ".tsx": "tsx", ".ts": "ts" },
};

if (isWatch) {
  esbuild.context(buildOptions).then(ctx => {
    ctx.watch();
    console.log("Watching for changes...");
  }).catch(() => process.exit(1));
} else {
  esbuild.build(buildOptions)
    .then(() => {
      console.log("Build complete.");
    })
    .catch(() => process.exit(1));
}
