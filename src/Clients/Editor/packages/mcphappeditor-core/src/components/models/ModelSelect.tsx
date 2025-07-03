import React from "react";
import { useTheme } from "../../ThemeContext";
import { useMediaQuery } from "usehooks-ts";

export interface ModelOption {
  id: string;
  displayName: string;
  publisher?: string;
}

interface ModelSelectProps {
  models: ModelOption[];
  value: string;
  onChange: (id: string) => void;
  disabled?: boolean;
}

export const ModelSelect: React.FC<ModelSelectProps> = ({
  models,
  value,
  onChange,
  disabled,
}) => {
  const { Select } = useTheme();
  const isDesktop = useMediaQuery("(min-width: 768px)");
  // Fallback to native select if theme does not provide one
  const SelectComponent = Select || "select";

  // Group models by publisher if any have publisher
  const hasGroups = models.some((m) => m.publisher);
  let grouped: Record<string, ModelOption[]> = {};
  let ungrouped: ModelOption[] = [];

  if (hasGroups) {
    grouped = models.reduce((acc, model) => {
      if (model.publisher) {
        if (!acc[model.publisher]) acc[model.publisher] = [];
        acc[model.publisher].push(model);
      } else {
        ungrouped.push(model);
      }
      return acc;
    }, {} as Record<string, ModelOption[]>);
  }

  return (
    <SelectComponent
      value={value}
      icon={"brain"}
      style={{ minWidth: isDesktop ? 260 : 200 }}
      size={"large"}
      onChange={(e: React.ChangeEvent<HTMLSelectElement> | any) => {
        // For Fluent UI Dropdown, e.target.value may not exist, so fallback to e.currentTarget.value or e (for custom)
        const selectedValue = e?.target?.value ?? e?.currentTarget?.value ?? e;
        onChange(selectedValue);
      }}
      disabled={disabled}
      aria-label="Model"
    >
      {hasGroups ? (
        <>
          {Object.entries(grouped).map(([publisher, models]) => (
            <optgroup key={publisher} label={publisher}>
              {models.map((model) => (
                <option key={model.id} value={model.id}>
                  {model.displayName}
                </option>
              ))}
            </optgroup>
          ))}
          {ungrouped.map((model) => (
            <option key={model.id} value={model.id}>
              {model.displayName}
            </option>
          ))}
        </>
      ) : (
        models.map((model) => (
          <option key={model.id} value={model.id}>
            {model.displayName}
          </option>
        ))
      )}
    </SelectComponent>
  );
};
