import type { McpServerWithName } from "mcphappey-types";
import { useTheme } from "../ThemeContext";
import { useMcpStore } from "mcphappey-state";
import { useMcpConnect } from "../hooks/useMcpConnect";

interface ServerCardProps {
  server: McpServerWithName;
  onShowDetails: (server: McpServerWithName) => void;
}

const ServerCard = ({ server, onShowDetails }: ServerCardProps) => {
  const { Button, Card } = useTheme();
  const { connect } = useMcpConnect();
  const { disconnect, isConnected } = useMcpStore();
  const connected = isConnected(server.url);

  return (
    <Card
      title={server.name}
      text={server.url}
      actions={
        <>
          <Button
            onClick={() =>
              connected ? disconnect(server.url) : connect(server.url)
            }
            variant={connected ? "success" : "primary"}
            size="sm"
            disabled={false}
          >
            {connected ? "Disconnect" : "Connect"}
          </Button>
          {connected && (
            <Button
              onClick={() => onShowDetails(server)}
              variant="secondary"
              size="sm"
            >
              Details
            </Button>
          )}
        </>
      }
    />
  );
};

export default ServerCard;
