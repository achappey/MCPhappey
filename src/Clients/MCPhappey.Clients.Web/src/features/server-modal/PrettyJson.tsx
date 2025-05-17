/**
 * PrettyJson - Utility component for pretty-printing JSON objects.
 * Pure function component, no React import.
 */
type PrettyJsonProps = {
  data: any;
};

export default function PrettyJson({ data }: PrettyJsonProps) {
  return (
    <pre style={{ margin: 0, fontSize: 13, background: "#f8f9fa" }}>
      {JSON.stringify(data, null, 2)}
    </pre>
  );
}
