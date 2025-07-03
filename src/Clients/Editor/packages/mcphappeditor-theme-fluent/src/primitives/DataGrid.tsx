import {
  DataGrid as FluentDataGrid,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridBody,
  DataGridRow,
  DataGridCell,
  TableCellLayout,
  createTableColumn,
  type TableColumnDefinition,
} from "@fluentui/react-components";
import type {
  DataGridProps,
  DataGridColumn,
} from "mcphappeditor-types/src/theme";

/**
 * Fluent DataGrid: feature-rich, themed, full parity with Fluent Table.
 */
export const DataGrid = <T,>({
  items,
  columns,
  keySelector,
  selectable,
  sortable,
  onSelectionChange,
  onSortChange,
  style,
  nativeProps,
}: DataGridProps<T>) => {
  // Map generic columns to Fluent TableColumnDefinition
  const fluentColumns: TableColumnDefinition<T>[] = columns.map((col) =>
    createTableColumn<T>({
      columnId: col.id,
      renderHeaderCell: () => col.header,
      renderCell: col.render,
      compare: col.sortable
        ? (a, b) => {
            const va = col.render(a);
            const vb = col.render(b);
            return String(va).localeCompare(String(vb));
          }
        : undefined,
    })
  );

  return (
    <FluentDataGrid
      items={items}
      columns={fluentColumns}
      sortable={sortable}
      selectionMode={
        (selectable === "multi"
          ? "multiselect"
          : selectable === "single"
          ? "single"
          : "false") as any
      }
      getRowId={keySelector as any}
      focusMode="composite"
      style={{ minWidth: 550, ...style }}
      {...nativeProps}
    >
      <DataGridHeader>
        <DataGridRow
          selectionCell={
            selectable
              ? { checkboxIndicator: { "aria-label": "Select all rows" } }
              : undefined
          }
        >
          {({ renderHeaderCell }) => (
            <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
          )}
        </DataGridRow>
      </DataGridHeader>
      <DataGridBody<T>>
        {({ item, rowId }) => (
          <DataGridRow<T>
            key={rowId}
            selectionCell={
              selectable
                ? { checkboxIndicator: { "aria-label": "Select row" } }
                : undefined
            }
          >
            {({ renderCell }) => (
              <DataGridCell>{renderCell(item)}</DataGridCell>
            )}
          </DataGridRow>
        )}
      </DataGridBody>
    </FluentDataGrid>
  );
};
