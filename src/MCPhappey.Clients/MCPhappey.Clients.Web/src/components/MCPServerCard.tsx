import Card from "react-bootstrap/Card";
import Button from "react-bootstrap/Button";
import OverlayTrigger from "react-bootstrap/OverlayTrigger";
import Tooltip from "react-bootstrap/Tooltip";
import { McpServerWithName } from "../hooks/useMcpServers";
import { useMcpClientContext } from "../context/McpClientContext";

import Alert from "react-bootstrap/Alert";
import Spinner from "react-bootstrap/Spinner";
import { ArrowRight, Clipboard, ClipboardPlus } from "react-bootstrap-icons";

interface MCPServerCardProps {
  server: McpServerWithName;
  onShowDetails: (server: McpServerWithName) => void;
  // onConnect is removed as the card handles its own connection via context
  // disabled is removed as it's derived from context's connecting state
}

const MCPServerCard = ({ server, onShowDetails }: MCPServerCardProps) => {
  const { connected, connecting, errors, connect, disconnect } =
    useMcpClientContext();

  const isConnected = connected[server.name] || false;
  const isLoading = connecting[server.name] || false;
  const serverError = errors[server.name] || null;

  const handleConnect = () => {
    connect(server);
  };

  const handleDisconnect = () => {
    disconnect(server);
  };

  return (
    <Card className="mb-3" style={{ minWidth: 300 }}>
      <Card.Body>
        <OverlayTrigger
          placement="left"
          overlay={
            <Tooltip id={`tooltip-url-${server.name}`}>{server.name}</Tooltip>
          }
        >
          <Card.Title
            style={{
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {server.name}
          </Card.Title>
        </OverlayTrigger>
        <OverlayTrigger
          placement="auto"
          overlay={
            <Tooltip id={`tooltip-url-${server.name}`}>{server.url}</Tooltip>
          }
        >
          <Card.Subtitle
            className="mb-2 text-muted"
            style={{
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {server.url}
          </Card.Subtitle>
        </OverlayTrigger>
        {isLoading && (
          <div className="d-flex align-items-center my-2">
            <Spinner animation="border" size="sm" role="status" />
            <span className="ms-2">Connecting...</span>
          </div>
        )}

        {serverError && !isLoading && (
          <Alert
            variant="danger"
            className="mt-2 p-2"
            style={{ fontSize: "0.8rem" }}
          >
            Error: {serverError}
          </Alert>
        )}

        <div className="d-flex gap-2 mt-2">
          {isConnected && (
            <Button
              variant="secondary"
              size="sm"
              onClick={() => onShowDetails(server)}
              disabled={isLoading}
            >
              Details
            </Button>
          )}
          {!isConnected && (
            <Button
              variant="primary"
              size="sm"
              onClick={handleConnect}
              disabled={isLoading}
            >
              Connect
            </Button>
          )}
          {isConnected && (
            <Button
              variant="danger" // Changed to danger for disconnect
              size="sm"
              onClick={handleDisconnect}
              disabled={isLoading}
            >
              Disconnect
            </Button>
          )}
          <OverlayTrigger
            placement="right"
            overlay={
              <Tooltip id={`tooltip-copy-${server.name}`}>
                Click to copy the url
              </Tooltip>
            }
          >
            <Button
              variant="outline-secondary"
              size="sm"
              onClick={() => navigator.clipboard.writeText(server.url)}
            >
              <ClipboardPlus />
            </Button>
          </OverlayTrigger>
        </div>
      </Card.Body>
    </Card>
  );
};

export default MCPServerCard;
