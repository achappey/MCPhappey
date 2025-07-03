import { useState, useMemo } from "react";
import { useAppStore } from "mcphappeditor-state";
import { Resource } from "mcphappeditor-mcp";
import { useTheme } from "../../ThemeContext";
import { ResourceSelectModal } from "./ResourceSelectModal";

type ResourceSelectButtonProps = {};

export const ResourceSelectButton = ({}: ResourceSelectButtonProps) => {
  const { Button } = useTheme();
  const readResource = useAppStore((s) => s.readResource);
  const resources = useAppStore((s) => s.resources);
  const selected = useAppStore((s) => s.selected);
  const servers = useAppStore((s) => s.servers);
  const [open, setOpen] = useState(false);

  const allResources = useMemo(
    () =>
      selected.flatMap((name) =>
        Array.isArray(resources[servers[name]?.url])
          ? resources[servers[name].url].filter(
              (r: Resource) =>
                !r.annotations || r.annotations.audience?.includes("user")
            )
          : []
      ),
    [selected, servers, resources]
  );

  if (allResources.length === 0) return null;

  return (
    <>
      <Button
        type="button"
        icon="resources"
        variant="transparent"
        onClick={() => setOpen(true)}
        title="Insert Resource"
      />
      {open && (
        <ResourceSelectModal
          resources={allResources}
          onSelect={async (uri) => {
            setOpen(false);
            await readResource(uri);
          }}
          onHide={() => setOpen(false)}
        />
      )}
    </>
  );
};
