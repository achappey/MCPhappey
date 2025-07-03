import type { DataGridProps } from "mcphappeditor-types/src/theme";

/**
 * Bootstrap DataGrid: simple table with optional selection.
 */
export const DataGrid = <T,>({
  items,
  columns,
  keySelector,
  selectable,
  onSelectionChange,
  style,
}: DataGridProps<T>) => {
  // Selection state (uncontrolled for now)
  return (
    <table className="table table-hover" style={style}>
      <thead>
        <tr>
          {selectable && <th></th>}
          {columns.map(col => (
            <th key={col.id}>{col.header}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {items.map(item => (
          <tr key={keySelector(item)}>
            {selectable && (
              <td>
                <input type={selectable === "multi" ? "checkbox" : "radio"} />
              </td>
            )}
            {columns.map(col => (
              <td key={col.id}>{col.render(item)}</td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
};