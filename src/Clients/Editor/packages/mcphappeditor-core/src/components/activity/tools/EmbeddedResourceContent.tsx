import { useTheme } from "../../../ThemeContext";
import { JsonViewer } from "../../common/JsonViewer";

interface EmbeddedResourceContentProps {
  resource: {
    uri?: string;
    mimeType?: string;
    text?: string;
    data?: string;
  };
}

export const EmbeddedResourceContent: React.FC<
  EmbeddedResourceContentProps
> = ({ resource }) => {
  const { mimeType, uri, text, data } = resource;
  const src = uri || data;
  const { Image } = useTheme();

  if (!mimeType && !src && !text) return <div>No content</div>;

  if (mimeType?.startsWith("image/") && src)
    return <Image fit={"contain"} src={src} />;
  if (mimeType?.startsWith("audio/") && src)
    return <audio src={src} controls style={{ width: "100%" }} />;

  if (mimeType?.startsWith("video/") && src)
    return (
      <video src={src} controls style={{ maxWidth: "100%", borderRadius: 8 }} />
    );

  if (mimeType === "application/json")
    return <JsonViewer value={text ?? data ?? ""} />;

  if (mimeType?.startsWith("text/"))
    return (
      <pre style={{ whiteSpace: "pre-wrap", fontFamily: "monospace" }}>
        {text ?? data ?? ""}
      </pre>
    );

  // fallback: just show link or whatever is available
  if (src)
    return (
      <a href={src} target="_blank" rel="noopener noreferrer">
        Open resource ({mimeType || "unknown type"})
      </a>
    );

  return <span>{text || "No displayable content"}</span>;
};
