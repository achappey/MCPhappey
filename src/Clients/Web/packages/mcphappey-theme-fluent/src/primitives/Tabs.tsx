import * as React from "react";
import { TabList, Tab as FluentTab } from "@fluentui/react-components";

interface TabsProps {
  activeKey: string;
  onSelect: (k: string) => void;
  className?: string;
  children: React.ReactNode;
}

export const Tabs: React.FC<TabsProps> = ({
  activeKey,
  onSelect,
  className,
  children,
}) => {
  const headers: React.ReactElement[] = [];
  let activePanel: React.ReactElement | null = null;

  React.Children.forEach(children, (child) => {
    if (!React.isValidElement(child)) return;
    const tab = child as React.ReactElement<{ eventKey: string; title: React.ReactNode; children: React.ReactNode }>;
    const { eventKey, title } = tab.props;
    headers.push(
      <FluentTab value={eventKey} key={eventKey}>
        {title}
      </FluentTab>
    );
    if (eventKey === activeKey) {
      activePanel = (
        <div style={{ padding: "1em 0" }}>{tab.props.children}</div>
      );
    }
  });

  return (
    <div className={className}>
      <TabList
        selectedValue={activeKey}
        onTabSelect={(_, data) => onSelect(data.value as string)}
      >
        {headers}
      </TabList>
      {activePanel}
    </div>
  );
};
