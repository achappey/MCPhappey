import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeRaw from "rehype-raw";
import rehypeSanitize, { defaultSchema } from "rehype-sanitize";
import { useTheme } from "../../ThemeContext";
import React from "react";

// Line-wrapping <pre> for code blocks
const WrappedPre = (props: any) => (
  <pre
    style={{
      whiteSpace: "pre-wrap",
      wordBreak: "break-word",
      overflowX: "auto",
      margin: "0.5em 0",
    }}
    {...props}
  />
);

// Extend sanitizer to allow <details> and <summary>
const schema = {
  ...defaultSchema,
  tagNames: [...(defaultSchema.tagNames ?? []), "details", "summary"],
  attributes: {
    ...defaultSchema.attributes,
    img: ["src", "alt", "title", "width", "height"], // ← overwrite
  },
  protocols: {
    ...defaultSchema.protocols,
    src: ["http", "https", "data"], // 👈 key line
  },
};

export const Markdown = ({ text }: { text: string }) => {
  const { Image } = useTheme();
  return (
    <ReactMarkdown
      remarkPlugins={[remarkGfm]}
      rehypePlugins={[rehypeRaw, [rehypeSanitize, schema]]}
      urlTransform={(uri) => uri}
      components={{
        pre: WrappedPre,
        p: ({ node, ...props }) => (
          <p style={{ margin: "0.5em 0" }} {...props} />
        ),
        img: ({ node, ...props }) => <Image {...props} fit="contain" />,
      }}
    >
      {text}
    </ReactMarkdown>
  );
};

export const MemoMarkdown = React.memo(Markdown);
