import { useMemo } from "react";
import { useConversations } from "mcphappeditor-conversations";
import { ImageGrid } from "../common/ImageGrid";
import { LibraryHeader } from "./LibraryHeader";

type ImageItem = {
  conversationId: string;
  messageId: string;
  createdAt: number;
  data: string;
  mimeType: string;
};

export const LibraryPage = () => {
  const conversations = useConversations();
  const images = useMemo(() => {
    const out: ImageItem[] = [];
    conversations.items.forEach((c: any) =>
      c.messages
        .filter((m: any) => m.role === "assistant")
        .forEach((m: any) =>
          (m.parts || [])
            .filter(
              (p: any) =>
                typeof p?.type === "string" &&
                p.type.startsWith("tool-") &&
                p.state === "output-available"
            )
            .forEach((p: any) =>
              (p.output?.content || [])
                .filter((x: any) => x.type === "image")
                .forEach((img: any) =>
                  out.push({
                    conversationId: c.id,
                    messageId: m.id,
                    createdAt: m.metadata?.timestamp ?? 0,
                    data: img.data,
                    mimeType: img.mimeType,
                  })
                )
            )
        )
    );

    return out.sort((a, b) => b.createdAt - a.createdAt);
  }, [conversations.items]);

  // const images: any[] = [];
  const latestFive = images.slice(0, 5);

  return (
    <div
      style={{
   //     minHeight: "100vh",
        background: "transparent",
        width: "100%",
      }}
    >
      <LibraryHeader />
      <div
        style={{
          maxWidth: "100%",
          margin: "0 auto",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
        }}
      >
        <ImageGrid
          items={images.map((item) => ({
            data: item.data,
            mimeType: item.mimeType,
            type: "image",
          }))}
          columns={5}
          gap="1rem"
          fit="cover"
          shape="square"
          shadow
          style={{ width: "100%" }}
        />
      </div>
    </div>
  );
};
