const esbuild = require("esbuild");

const isWatch = process.argv.includes("--watch");

const MCP_SERVERS_API_URL =
  process.env.MCP_SERVERS_API_URL || "http://localhost:3001/mcp.json";

const buildOptions = {
  entryPoints: ["src/main.tsx"],
  bundle: true,
  outfile: "public/bundle.js",
  sourcemap: true,
  minify: false,
  define: {
    "process.env.NODE_ENV": '"development"',
    "__MCP_SERVERS_API_URL__": JSON.stringify(MCP_SERVERS_API_URL),
  },
  loader: { ".tsx": "tsx", ".ts": "ts" }
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
