import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import * as React from "react";
import { TabList, Tab as FluentTab } from "@fluentui/react-components";
export const Tabs = ({ activeKey, onSelect, className, children, }) => {
    const headers = [];
    let activePanel = null;
    React.Children.forEach(children, (child) => {
        if (!React.isValidElement(child))
            return;
        const tab = child;
        const { eventKey, title } = tab.props;
        headers.push(_jsx(FluentTab, { value: eventKey, children: title }, eventKey));
        if (eventKey === activeKey) {
            activePanel = (_jsx("div", { style: { padding: "1em 0" }, children: tab.props.children }));
        }
    });
    return (_jsxs("div", { className: className, children: [_jsx(TabList, { selectedValue: activeKey, onTabSelect: (_, data) => onSelect(data.value), children: headers }), activePanel] }));
};
//# sourceMappingURL=Tabs.js.map