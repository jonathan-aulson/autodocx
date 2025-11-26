import { Icons } from '@/components/Icons/Icons';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuCheckboxItem,
    DropdownMenuContent,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import type { Table } from '@tanstack/react-table';

interface DataTableViewOptionsProps<TData> {
    table: Table<TData>;
}

function extractTitleFromHeader(headerFn: Function, column: any): string {
    const headerComponent = headerFn({ column });
    return headerComponent?.props?.title || headerComponent?.props?.children?.props?.title || formatColumnName(column.id);
}

export function DataTableViewOptions<TData>({ table }: DataTableViewOptionsProps<TData>) {
    return (
        <DropdownMenu>
        <DropdownMenuTrigger asChild>
            <Button
                className="ml-auto hidden h-8 lg:flex"
                size="sm"
                variant="outline"
                data-qa-id="button-viewColumns"
            >
                <Icons.slidersVertical className="mr-2 size-4" />
                View
            </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-[150px]" data-qa-id="dropdown-columnVisibility">
            <DropdownMenuLabel>Toggle columns</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {table
                .getAllColumns()
                .filter(
                    (column) =>
                        typeof column.accessorFn !== 'undefined' && column.getCanHide(),
                )
                .map((column) => (
                    <DropdownMenuCheckboxItem
                        checked={column.getIsVisible()}
                        className="capitalize"
                        key={column.id}
                        onCheckedChange={(value) => {
                            column.toggleVisibility(Boolean(value));
                        }}
                        data-qa-id={`checkbox-column-${column.id}`}
                    >
                        {typeof column.columnDef.header === 'function'
                            ? extractTitleFromHeader(column.columnDef.header, column)
                            : formatColumnName(column.id)}
                    </DropdownMenuCheckboxItem>
                ))}
        </DropdownMenuContent>
    </DropdownMenu>
    );
}

function formatColumnName(columnId: string): string {
    return columnId.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, str => str.toUpperCase());
}
