import Table from "react-bootstrap/Table";
import Spinner from "react-bootstrap/Spinner";
import Alert from "react-bootstrap/Alert";

type MCPServerResourcesTabProps = {
  capabilities: any | null;
  resources: any[] | null;
  loading: boolean;
  error: string | null;
};

export default function MCPServerResourcesTab({
  capabilities,
  resources,
  loading,
  error,
}: MCPServerResourcesTabProps) {
  if (!capabilities?.resources) return <div>Resources not supported.</div>;
  if (loading) return <Spinner animation="border" size="sm" />;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!resources) return <span>—</span>;
  if (resources.length === 0) return <span>No resources available.</span>;
  return (
    <Table bordered size="sm">
      <thead>
        <tr>
          <th>Name</th>
          <th>URI</th>
          <th>Description</th>
          <th>MIME Type</th>
        </tr>
      </thead>
      <tbody>
        {resources.map((r: any) => (
          <tr key={r.uri}>
            <td>{r.name || <span style={{ color: "#888" }}>—</span>}</td>
            <td style={{ wordBreak: "break-all" }}>{r.uri}</td>
            <td>{r.description || <span style={{ color: "#888" }}>—</span>}</td>
            <td>{r.mimeType || <span style={{ color: "#888" }}>—</span>}</td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
}
