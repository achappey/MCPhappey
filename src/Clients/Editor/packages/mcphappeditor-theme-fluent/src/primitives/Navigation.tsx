import * as React from "react";
import {
  Cloud24Regular,
  Database24Regular,
  MoreHorizontalRegular,
} from "@fluentui/react-icons";
import {
  AppItem,
  Button,
  Hamburger,
  Input,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  NavDivider,
  NavDrawer,
  NavDrawerBody,
  NavDrawerHeader,
  NavItem,
  NavSectionHeader,
  Tooltip,
} from "@fluentui/react-components";
import { makeStyles } from "@fluentui/react-components";
import { IconToken, NavigationProps } from "mcphappeditor-types";
import { useState } from "react";
import { iconMap } from "./Button";

const useStyles = makeStyles({
  root: {
    overflow: "hidden",
    display: "flex",
    height: "100%",
    flexDirection: "column",
  },
  nav: { minWidth: "220px", height: "100%" },
  headerBar: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    width: "100%",
  },
  rightIcons: { display: "flex", alignItems: "center" },
  iconBtn: {
    cursor: "pointer",
    background: "none",
    border: "none",
    color: "inherit",
  },
  navItemContent: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    width: "100%",
  },
});

export const Navigation: React.FC<NavigationProps> = ({
  items,
  activeKey,
  onSelect,
  storageType = "local",
  onStorageSwitch,
  onDelete,
  onClose,
  isOpen,
  onRename,
  multiple = false,
  drawerType = "inline",
  className,
  style,
}) => {
  const styles = useStyles();
  //const [open, setOpen] = React.useState(isOpen);

  const appItem = items.length && items[0].key === "app" ? items[0] : null;
  const navItems = appItem ? items.slice(1) : items;
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editValue, setEditValue] = useState("");

  // Icon for storage
  const StorageIcon =
    storageType === "local" ? Database24Regular : Cloud24Regular;

  return (
    <div className={styles.root} style={style}>
      <NavDrawer
        open={isOpen}
        type={drawerType}
        multiple={multiple}
        onOpenChange={
          onClose
            ? (event, data) => (data.open ? undefined : onClose())
            : undefined
        }
        size="small"
        defaultSelectedValue={activeKey}
        className={styles.nav + (className ? " " + className : "")}
      >
        <NavDrawerHeader>
          <div className={styles.headerBar}>
            {/* Hamburger left */}
            <Tooltip relationship="label" content="Close navigation">
              <span>
                <Hamburger onClick={onClose ? () => onClose() : undefined} />
              </span>
            </Tooltip>
            {/* Right icon buttons */}
            <div className={styles.rightIcons}>
              {/* Storage Switch */}
              {onStorageSwitch != undefined ? (
                <Tooltip
                  relationship="label"
                  content={`Opslag: ${
                    storageType === "local" ? "Lokaal" : "Cloud"
                  }`}
                >
                  <Button
                    aria-label="Wissel opslag"
                    icon={<StorageIcon />}
                    appearance="transparent"
                    onClick={() => {
                      onStorageSwitch(
                        storageType === "local" ? "remote" : "local"
                      );
                    }}
                    type="button"
                  ></Button>
                </Tooltip>
              ) : null}
            </div>
          </div>
        </NavDrawerHeader>
        <NavDrawerBody>
          {appItem && (
            <AppItem as="a" href={appItem.href}>
              {appItem.label}
            </AppItem>
          )}
          {navItems.map((item, idx) =>
            item.key === "divider" ? (
              <NavDivider key={idx} />
            ) : item.key.startsWith("section:") ? (
              <NavSectionHeader key={item.key}>{item.label}</NavSectionHeader>
            ) : (
              <NavItem
                key={item.key}
                style={{ paddingTop: 4, paddingBottom: 4 }}
                icon={
                  item.icon && iconMap[item.icon as IconToken] ? (
                    <span
                      style={{
                        display: "flex",
                        alignItems: "center",
                      }}
                    >
                      {React.createElement(iconMap[item.icon as IconToken], {
                        style: { fontSize: 24, display: "block" },
                      })}
                    </span>
                  ) : null
                }
                value={item.key}
                disabled={item.disabled}
                onClick={() =>
                  item.onClick ? item.onClick() : onSelect && onSelect(item.key)
                }
              >
                {editingId === item.key ? (
                  <>
                    {onRename && (
                      <Input
                        value={editValue}
                        autoFocus
                        onChange={(e: any) => setEditValue(e.target.value)}
                        onBlur={async () => await onRename(item.key, editValue)}
                        onKeyDown={async (e: any) => {
                          if (e.key === "Enter")
                            await onRename(item.key, editValue);
                        }}
                      />
                    )}
                  </>
                ) : (
                  <span className={styles.navItemContent}>
                    <span
                      style={{
                        whiteSpace: "nowrap",
                        textOverflow: "ellipsis",
                        overflow: "hidden",
                      }}
                    >
                      {item.label}
                    </span>
                    {item.conversationItem && (
                      <Menu>
                        <MenuTrigger disableButtonEnhancement>
                          <Button
                            size="small"
                            appearance="transparent"
                            icon={<MoreHorizontalRegular />}
                            onClick={(e) => e.stopPropagation()}
                          />
                        </MenuTrigger>
                        <MenuPopover>
                          <MenuList>
                            <MenuItem
                              onClick={() => {
                                /* Bewerk-actie */
                              }}
                            >
                              Bewerken
                            </MenuItem>
                            {onDelete && (
                              <MenuItem
                                onClick={async (e) => {
                                  e.stopPropagation();
                                  await onDelete(item.key);
                                }}
                              >
                                Verwijderen
                              </MenuItem>
                            )}
                          </MenuList>
                        </MenuPopover>
                      </Menu>
                    )}
                  </span>
                )}
              </NavItem>
            )
          )}
        </NavDrawerBody>
      </NavDrawer>
    </div>
  );
};

export default Navigation;
