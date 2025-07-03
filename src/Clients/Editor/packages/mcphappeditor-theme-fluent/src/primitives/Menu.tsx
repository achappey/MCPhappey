// AppMenuFluent.tsx
import {
  Menu as FMenu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Button,
} from "@fluentui/react-components";
import { MoreHorizontalRegular } from "@fluentui/react-icons";
import { MenuProps } from "mcphappeditor-types";

export const Menu = ({
  items,
  trigger,
  size = "small",
  className,
}: MenuProps) => (
  <FMenu>
    <MenuTrigger disableButtonEnhancement>
      {trigger || (
        <Button
          size={size}
          appearance="transparent"
          icon={<MoreHorizontalRegular />}
          onClick={(e) => e.stopPropagation()}
          className={className}
        />
      )}
    </MenuTrigger>
    <MenuPopover>
      <MenuList>
        {items.map((item) => (
          <MenuItem
            key={item.key}
            onClick={item.onClick}
            style={item.danger ? { color: "red" } : undefined}
          >
            {item.label}
          </MenuItem>
        ))}
      </MenuList>
    </MenuPopover>
  </FMenu>
);
