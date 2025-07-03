import { useState, useEffect } from "react";
import { useTheme } from "../../ThemeContext";
import { useAppStore } from "mcphappeditor-state";
import { ToolInvocationsActivity } from "./tools/ToolInvocationsActivity";
import { ProgressNotificationsActivity } from "../activity/progress/ProgressNotificationsActivity";
import { SamplingActivity } from "../activity/sampling/SamplingActivity";
import { LoggingNotificationsActivity } from "../activity/logging/LoggingNotificationsActivity";
import { useToolInvocations } from "./tools/useToolInvocations";

export const ActivityDrawer = () => {
  const { Drawer, Tabs, Tab } = useTheme();
  const showActivities = useAppStore((s) => s.showActivities);
  const setActivities = useAppStore((s) => s.setActivities);
  const toolInvocations = useToolInvocations();
  const progress = useAppStore((s) => s.progress);
  const sampling = useAppStore((s) => s.sampling);
  const notifications = useAppStore((s) => s.notifications);

  const disabled = {
    tools: !toolInvocations.length,
    progress: !progress.length,
    sampling: !Object.keys(sampling).length,
    logging: !notifications.length,
  };

  const tabOrder = [
    {
      key: "toolInvocations",
      label: "Tools",
      disabled: disabled.tools,
      component: ToolInvocationsActivity,
    },
    {
      key: "mcpProgress",
      label: "Progress",
      disabled: disabled.progress,
      component: ProgressNotificationsActivity,
    },
    {
      key: "mcpSampling",
      label: "Sampling",
      disabled: disabled.sampling,
      component: SamplingActivity,
    },
    {
      key: "mcpLogging",
      label: "Log",
      disabled: disabled.logging,
      component: LoggingNotificationsActivity,
    },
  ];

  const firstEnabledTab =
    tabOrder.find((t) => !t.disabled)?.key || "toolInvocations";
  const [activeTab, setActiveTab] = useState(firstEnabledTab);

  return (
    <Drawer
      open={showActivities}
      title="Activities"
      size="medium"
      onClose={() => setActivities(false)}
    >
      <Tabs activeKey={activeTab} onSelect={setActiveTab}>
        {tabOrder.map((tab) => (
          <Tab key={tab.key} eventKey={tab.key} title={tab.label}>
            {activeTab === tab.key ? <tab.component /> : null}
          </Tab>
        ))}
      </Tabs>
    </Drawer>
  );
};
