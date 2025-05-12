import Table from "react-bootstrap/Table";
import Badge from "react-bootstrap/Badge";
import Spinner from "react-bootstrap/Spinner";
import Alert from "react-bootstrap/Alert";
import PrettyJson from "./PrettyJson";
import { McpServerWithName } from "../hooks/useMcpServers";

type MCPServerSummaryProps = {
  server: McpServerWithName;
  loading: boolean;
  error: string | null;
  capabilities: any | null;
};

export default function MCPServerSummary({
  server,
  loading,
  error,
  capabilities,
}: MCPServerSummaryProps) {
  return (
    <>
      <Table borderless size="sm">
        <tbody>
          <tr>
            <th>Type</th>
            <td>{server.type}</td>
          </tr>
          <tr>
            <th>URL</th>
            <td style={{ wordBreak: "break-all" }}>{server.url}</td>
          </tr>
          {server.headers && (
            <tr>
              <th>Headers</th>
              <td>
                <PrettyJson data={server.headers} />
              </td>
            </tr>
          )}
          {/* Render any other fields */}
          {Object.entries(server)
            .filter(([key]) => !["name", "type", "url", "headers"].includes(key))
            .map(([key, value]) => (
              <tr key={key}>
                <th>{key}</th>
                <td>
                  {typeof value === "object" ? (
                    <PrettyJson data={value} />
                  ) : (
                    String(value)
                  )}
                </td>
              </tr>
            ))}
        </tbody>
      </Table>
      <hr />
      <h6>Capabilities</h6>
      {loading ? (
        <Spinner animation="border" size="sm" />
      ) : error ? (
        <Alert variant="danger">{error}</Alert>
      ) : capabilities ? (
        <div style={{ display: "flex", gap: 12 }}>
          {capabilities.prompts && (
            <Badge bg="primary">Prompts</Badge>
          )}
          {capabilities.resources && (
            <Badge bg="success">Resources</Badge>
          )}
          {capabilities.tools && (
            <Badge bg="warning" text="dark">Tools</Badge>
          )}
          {!capabilities.prompts && !capabilities.resources && !capabilities.tools && (
            <span>No primitives supported</span>
          )}
        </div>
      ) : (
        <span>—</span>
      )}
    </>
  );
}
