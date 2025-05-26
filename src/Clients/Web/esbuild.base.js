const isWatch = process.argv.includes("--watch");
module.exports = (entry, outfile) => ({
  entryPoints: [entry],
  bundle: true,
  outfile,
  sourcemap: true,
  minify: !isWatch,
  external: ["react", "react-dom"],
  loader: { ".ts": "ts", ".tsx": "tsx" }
});
