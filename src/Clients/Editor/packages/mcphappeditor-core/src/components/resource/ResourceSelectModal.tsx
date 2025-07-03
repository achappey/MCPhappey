import { useTheme } from "../../ThemeContext";
import { useTranslation } from "mcphappeditor-i18n";
import { CancelButton } from "../common/CancelButton";
//import type { Resource } from "./ResourceSelectButton";
import { ResourceCard } from "./ResourceCard";
import { Resource } from "mcphappeditor-mcp";

export type ResourceSelectModalProps = {
  resources: Resource[];
  onSelect: (uri: string) => void;
  onHide: () => void;
};

export const ResourceSelectModal = ({
  resources,
  onSelect,
  onHide,
}: ResourceSelectModalProps) => {
  const { Modal } = useTheme();
  const { t } = useTranslation();

  return (
    <Modal
      show={true}
      onHide={onHide}
      actions={<CancelButton onClick={onHide} />}
      title={t("mcp.resources")}
    >
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: 8,
        }}
      >
        {resources.map((resource, idx) => (
          <ResourceCard
            key={resource.name + idx}
            resource={resource}
            onSelect={() => onSelect(resource.uri)}
          />
        ))}
      </div>
    </Modal>
  );
};
