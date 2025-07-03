import React from "react";
import { useTheme } from "../../ThemeContext";


// Props interface
interface ProtocolPageHeaderProps {
  title: string;
  officialUrl: string;
  docsUrl: string;
}

export const ProtocolPageHeader: React.FC<ProtocolPageHeaderProps> = ({
  title,
  officialUrl,
  docsUrl,
}) => {
  const { Header, Button } = useTheme();

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        width: "100%",
      }}
    >
      <div style={{ flex: 1 }} />
      <Header style={{ flex: "none", textAlign: "center", margin: 0 }}>
        {title}
      </Header>
      <div
        style={{
          flex: 1,
          display: "flex",
          justifyContent: "flex-end",
          gap: 8,
        }}
      >
        <Button
          size="small"
          variant="subtle"
          icon={"bookOpen"}
          aria-label="Technische documentatie"
          onClick={() => window.open(docsUrl, "_blank", "noopener")}
        />
        <Button
          size="small"
          variant="subtle"
          icon={"globe"}
          aria-label="Officiële site"
          onClick={() => window.open(officialUrl, "_blank", "noopener")}
        />
      </div>
    </div>
  );
};
