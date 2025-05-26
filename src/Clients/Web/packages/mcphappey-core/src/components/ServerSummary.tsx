import type { McpServerWithName, McpCapabilitySummary } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import PrettyJson from "./PrettyJson";

type ServerSummaryProps = {
  server: McpServerWithName;
  loading: boolean;
  error: string | null | undefined;
  promptCount?: number;
  toolCount?: number;
  resourceCount?: number;
  capabilities: McpCapabilitySummary | null | undefined;
};

const ServerSummary = ({
  server,
  loading,
  promptCount,
  resourceCount,
  toolCount,
  error,
  capabilities,
}: ServerSummaryProps) => {
  const { Table, Badge, Spinner, Alert } = useTheme();
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
          {Object.entries(server)
            .filter(
              ([key]) => !["name", "type", "url", "headers"].includes(key)
            )
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
        <Spinner size="sm" />
      ) : error ? (
        <Alert variant="danger">{error}</Alert>
      ) : capabilities ? (
        <div style={{ display: "flex", gap: 12 }}>
          {capabilities.prompts && (
            <Badge bg="primary">{promptCount} prompt(s)</Badge>
          )}
          {capabilities.resources && (
            <Badge bg="success">{resourceCount} resource(s)</Badge>
          )}
          {capabilities.tools && (
            <Badge bg="warning">{toolCount} tool(s)</Badge>
          )}
          {!capabilities.prompts &&
            !capabilities.resources &&
            !capabilities.tools && <span>No primitives supported</span>}
        </div>
      ) : (
        <span>—</span>
      )}
    </>
  );
};

export default ServerSummary;
