import Table from "react-bootstrap/Table";
import Spinner from "react-bootstrap/Spinner";
import Alert from "react-bootstrap/Alert";
import PrettyJson from "./PrettyJson";

type MCPServerToolsTabProps = {
  capabilities: any | null;
  tools: any[] | null;
  loading: boolean;
  error: string | null;
};

export default function MCPServerToolsTab({
  capabilities,
  tools,
  loading,
  error,
}: MCPServerToolsTabProps) {
  if (!capabilities?.tools) return <div>Tools not supported.</div>;
  if (loading) return <Spinner animation="border" size="sm" />;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!tools) return <span>—</span>;
  if (tools.length === 0) return <span>No tools available.</span>;
  return (
    <Table bordered size="sm">
      <thead>
        <tr>
          <th>Name</th>
          <th>Description</th>
          <th>Input Schema</th>
        </tr>
      </thead>
      <tbody>
        {tools.map((t: any) => (
          <tr key={t.name}>
            <td><b>{t.name}</b></td>
            <td>{t.description || <span style={{ color: "#888" }}>—</span>}</td>
            <td>
              <PrettyJson data={t.inputSchema} />
            </td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
}
