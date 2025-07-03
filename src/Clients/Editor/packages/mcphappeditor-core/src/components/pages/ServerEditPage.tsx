import { useState } from "react";
import { useTheme } from "../../ThemeContext";
import type { DataGridColumn } from "mcphappeditor-types/src/theme";

type Server = {
  id: string;
  name: string;
  baseUrl: string;
  description: string;
};

type Prompt = { id: string; name: string; description: string };
type Resource = { id: string; type: string; uri: string };
type Tool = { id: string; name: string; description: string; endpoint: string };

export const ServerEditPage = () => {
  // Destructure themed components
  const {
    Input,
    TextArea,
    Button,
    Header,
    Tabs,
    Tab,
    DataGrid,
  } = useTheme();

  const [server, setServer] = useState<Server>({
    id: "srv-1",
    name: "Example Server",
    baseUrl: "https://api.example.com",
    description: "Demo server",
  });
  const [tab, setTab] = useState("general");
  const [prompts] = useState<Prompt[]>([]);
  const [resources] = useState<Resource[]>([]);
  const [tools] = useState<Tool[]>([]);

  const generalForm = (
    <form style={{ maxWidth: 480 }}>
      <div>
        <label>ID</label>
        <Input value={server.id} readOnly style={{ width: "100%" }} />
      </div>
      <div>
        <label>Name</label>
        <Input
          value={server.name}
          onChange={e => setServer(s => ({ ...s, name: e.target.value }))}
          style={{ width: "100%" }}
        />
      </div>
      <div>
        <label>Base URL</label>
        <Input
          value={server.baseUrl}
          onChange={e => setServer(s => ({ ...s, baseUrl: e.target.value }))}
          style={{ width: "100%" }}
        />
      </div>
      <div>
        <label>Description</label>
        <TextArea
          value={server.description}
          onChange={v => setServer(s => ({ ...s, description: v }))}
          rows={3}
          style={{ width: "100%" }}
        />
      </div>
      <div style={{ marginTop: 16, display: "flex", gap: 8 }}>
        <Button type="submit" variant="primary">
          Save
        </Button>
        <Button type="button" variant="danger">
          Delete
        </Button>
      </div>
    </form>
  );

  const promptColumns: DataGridColumn<Prompt>[] = [
    { id: "id", header: "ID", render: p => p.id },
    { id: "name", header: "Name", render: p => p.name },
    { id: "description", header: "Description", render: p => p.description },
  ];
  const resourceColumns: DataGridColumn<Resource>[] = [
    { id: "id", header: "ID", render: r => r.id },
    { id: "type", header: "Type", render: r => r.type },
    { id: "uri", header: "URI", render: r => r.uri },
  ];
  const toolColumns: DataGridColumn<Tool>[] = [
    { id: "id", header: "ID", render: t => t.id },
    { id: "name", header: "Name", render: t => t.name },
    { id: "description", header: "Description", render: t => t.description },
    { id: "endpoint", header: "Endpoint", render: t => t.endpoint },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Header level={2}>Edit MCP Server</Header>
      <Tabs activeKey={tab} onSelect={setTab}>
        <Tab eventKey="general" title="General">
          {generalForm}
        </Tab>
        <Tab eventKey="prompts" title="Prompts">
          <DataGrid
            items={prompts}
            columns={promptColumns}
            keySelector={p => p.id}
            selectable="multi"
            sortable
          />
        </Tab>
        <Tab eventKey="resources" title="Resources">
          <DataGrid
            items={resources}
            columns={resourceColumns}
            keySelector={r => r.id}
            selectable="multi"
            sortable
          />
        </Tab>
        <Tab eventKey="tools" title="Tools">
          <DataGrid
            items={tools}
            columns={toolColumns}
            keySelector={t => t.id}
            selectable="multi"
            sortable
          />
        </Tab>
      </Tabs>
    </div>
  );
};
