import * as React from "react";
import {
  Dropdown,
  Option,
  OptionGroup,
  DropdownProps,
  Field,
} from "@fluentui/react-components";
import type { IconToken } from "mcphappeditor-types";
import { Fragment } from "react";
import { iconMap } from "./Button";
import { useDarkMode } from "usehooks-ts";

interface SelectProps
  extends Omit<DropdownProps, "children" | "onChange" | "value"> {
  value: string;
  valueTitle?: string;
  label?: string;
  hint?: string;
  required?: boolean;
  onChange: (e: any) => void;
  disabled?: boolean;
  icon?: IconToken;
  children: React.ReactNode;
  style?: React.CSSProperties;
  "aria-label"?: string;
}

// Recursively render Option/OptionGroup for Fluent
function renderFluentOptions(children: React.ReactNode): React.ReactNode {
  return React.Children.map(children, (child) => {
    if (!React.isValidElement(child)) return null;
    const el = child as React.ReactElement<any>;

    if (el.type === Fragment) {
      return renderFluentOptions(el.props.children);
    }
    if (el.type === "option") {
      return (
        <Option key={el.props.value} value={el.props.value}>
          {el.props.children}
        </Option>
      );
    } else if (el.type === "optgroup") {
      return (
        <OptionGroup key={el.props.label} label={el.props.label}>
          {renderFluentOptions(el.props.children)}
        </OptionGroup>
      );
    }
    return null;
  });
}

export const Select: React.FC<SelectProps> = ({
  value,
  onChange,
  disabled,
  size,
  valueTitle,
  label,
  hint,
  required,
  icon,
  children,
  style,
  "aria-label": ariaLabel,
  ...rest
}) => {
  const { isDarkMode } = useDarkMode();
  const IconElement = icon ? iconMap[icon as IconToken] : undefined;

  const dropDownElement = (
    <Dropdown
      selectedOptions={[value]}
      value={valueTitle ?? value}
      size={size}
      expandIcon={IconElement ? <IconElement /> : undefined}
      onOptionSelect={(_, data) => {
        if (data.optionValue) onChange(data.optionValue);
      }}
      disabled={disabled}
      style={{ backgroundColor: isDarkMode ? "#141414" : undefined, ...style }}
      aria-label={ariaLabel}
      {...rest}
    >
      {renderFluentOptions(children)}
    </Dropdown>
  );

  return label ? (
    <Field label={label} hint={hint} required={required}>
      {dropDownElement}
    </Field>
  ) : (
    dropDownElement
  );
};
