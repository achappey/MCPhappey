import { useTheme } from "../../ThemeContext";
import type { DataGridProps } from "mcphappeditor-types/src/theme";

/**
 * Generic DataGrid wrapper. Uses theme.DataGrid if present, else falls back to a basic table.
 */
export const DataGrid = <T,>(props: DataGridProps<T>) => {
  const theme = useTheme();
  if (theme.DataGrid) {
    return theme.DataGrid<T>(props);
  }
  // Fallback: minimal table
  return (
    <table style={props.style}>
      <thead>
        <tr>
          {props.columns.map(col => (
            <th key={col.id}>{col.header}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {props.items.map(item => (
          <tr key={props.keySelector(item)}>
            {props.columns.map(col => (
              <td key={col.id}>{col.render(item)}</td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
};