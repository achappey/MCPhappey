import { TextContentView } from "./TextContentView";
import { ImageContentView } from "./ImageContentView";
import { AudioContentView } from "./AudioContentView";
import { ResourceLinkView } from "./ResourceLinkView";
import { EmbeddedResourceView } from "./EmbeddedResourceView";

interface ContentBlockViewProps {
  block: any;
}

export const ContentBlockView = ({ block }: ContentBlockViewProps) => {
  if (!block || typeof block !== "object" || !block.type) {
    return null;
  }
  switch (block.type) {
    case "text":
      return <TextContentView block={block} />;
    case "image":
      return <ImageContentView block={block} />;
    case "audio":
      return <AudioContentView block={block} />;
    case "resource_link":
      return <ResourceLinkView block={block} />;
    case "resource":
      return <EmbeddedResourceView block={block} />;
    default:
      return (
        <div style={{ color: "#888", fontSize: 13 }}>
          Unknown content type: {block.type}
        </div>
      );
  }
};