import Table from "react-bootstrap/Table";
import Spinner from "react-bootstrap/Spinner";
import Alert from "react-bootstrap/Alert";
import Badge from "react-bootstrap/Badge";

type MCPServerPromptsTabProps = {
  capabilities: any | null;
  prompts: any[] | null;
  loading: boolean;
  error: string | null;
};

export default function MCPServerPromptsTab({
  capabilities,
  prompts,
  loading,
  error,
}: MCPServerPromptsTabProps) {
  if (!capabilities?.prompts) return <div>Prompts not supported.</div>;
  if (loading) return <Spinner animation="border" size="sm" />;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!prompts) return <span>—</span>;
  if (prompts.length === 0) return <span>No prompts available.</span>;
  return (
    <Table bordered size="sm">
      <thead>
        <tr>
          <th>Name</th>
          <th>Description</th>
          <th>Arguments</th>
        </tr>
      </thead>
      <tbody>
        {prompts.map((p: any) => (
          <tr key={p.name}>
            <td><b>{p.name}</b></td>
            <td>{p.description || <span style={{ color: "#888" }}>—</span>}</td>
            <td>
              {p.arguments && p.arguments.length > 0 ? (
                <ul style={{ margin: 0, paddingLeft: 18 }}>
                  {p.arguments.map((a: any) => (
                    <li key={a.name}>
                      <b>{a.name}</b>
                      {a.required && <Badge bg="danger" style={{ marginLeft: 4 }}>required</Badge>}
                      <span style={{ marginLeft: 6, color: "#666" }}>{a.description}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <span style={{ color: "#888" }}>None</span>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
}
