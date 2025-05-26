/**
 * Pretty prints a JSON object as a <pre> block.
 */
const PrettyJson = ({ data }: { data: any }) => (
  <pre className="mcph-pretty-json" style={{ fontSize: 12, background: "#f8f9fa", padding: 8, borderRadius: 4, overflowX: "auto" }}>
    {JSON.stringify(data, null, 2)}
  </pre>
);

export default PrettyJson;
