import { Button } from "@/components/ui/button";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";
import type { Column } from '@tanstack/react-table';
import { ArrowDown, ArrowUp, ChevronsUpDown, EyeOff } from "lucide-react";

interface DataTableColumnHeaderProps<TData, TValue> {
    column: Column<TData, TValue>;
    title: string;
    className?: string;
}

export function DataTableColumnHeader<TData, TValue>({
    column,
    title,
    className,
}: DataTableColumnHeaderProps<TData, TValue>) {
    if (!column.getCanSort()) {
        return <div className={cn(className)}>{title}</div>
    }

    return (
        <div className={cn("flex items-center space-x-2", className)}>
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button
                    variant="ghost"
                    size="sm"
                    className="-ml-3 h-8 data-[state=open]:bg-accent"
                    data-qa-id={`button-columnSort-${title}`}
                >
                    <span>{title}</span>
                    {column.getIsSorted() === "desc" ? (
                        <ArrowDown className="h-3.5 w-3.5" />
                    ) : column.getIsSorted() === "asc" ? (
                        <ArrowUp className="h-3.5 w-3.5" />
                    ) : (
                        <ChevronsUpDown className="h-3.5 w-3.5" />
                    )}
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" data-qa-id={`dropdown-columnOptions-${title}`}>
                <DropdownMenuItem onClick={() => column.toggleSorting(false)} data-qa-id={`dropdown-item-sortAsc-${title}`}>
                    <ArrowUp className="h-3.5 w-3.5 text-muted-foreground/70" />
                    Asc
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => column.toggleSorting(true)} data-qa-id={`dropdown-item-sortDesc-${title}`}>
                    <ArrowDown className="h-3.5 w-3.5 text-muted-foreground/70" />
                    Desc
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => column.toggleVisibility(false)} data-qa-id={`dropdown-item-hideColumn-${title}`}>
                    <EyeOff className="h-3.5 w-3.5 text-muted-foreground/70" />
                    Hide
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>
    </div>
    )
}