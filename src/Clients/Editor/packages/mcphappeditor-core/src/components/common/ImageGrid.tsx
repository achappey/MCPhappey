import { useTheme } from "../../ThemeContext";

type ImageItem = {
  data: string;
  mimeType: string;
  type: "image";
};

type ImageGridProps = {
  items: ImageItem[];
  columns?: number;
  gap?: number | string;
  fit?: "contain" | "cover";
  shape?: "square" | "rounded" | "circular";
  shadow?: boolean;
  style?: React.CSSProperties;
};

export const ImageGrid = ({
  items,
  columns,
  gap,
  fit,
  shape,
  shadow,
  style,
}: ImageGridProps) => {
  const { Image } = useTheme();
  const colCount = columns && columns > 0 ? columns : undefined;
  const gridTemplate =
    colCount != null
      ? `repeat(${colCount}, 1fr)`
      : "repeat(auto-fill, minmax(200px, 1fr))";
  const gridGap = gap ?? "1rem";

  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: gridTemplate,
        gap: gridGap,
        ...style,
      }}
    >
      {items.map((item, idx) => (
        <div
          key={idx}
          style={{
            width: "100%",
            aspectRatio: "1 / 1",
            position: "relative",
            overflow: "hidden",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Image
            src={`data:${item.mimeType};base64,${item.data}`}
            fit={fit}
            shape={shape}
            shadow={shadow}
          />
        </div>
      ))}
    </div>
  );
};