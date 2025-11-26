import { useState, useEffect } from "react";

export interface DragCell {
    rowIndex: number;
    colIndex: number;
}

interface UseDragAndCopyOptions<TContext = any> {
    onCopy?: (cells: DragCell[], context: TContext) => string[];
    onPaste?: (cells: DragCell[], clipboard: string[], context: TContext) => void;
    context?: TContext;
    activeCell?: DragCell | null;
    rowCount?: number;
    colCount?: number;
}

export default function useDragAndCopy<TContext = any>({
    onCopy,
    onPaste,
    context,
    activeCell,
    rowCount,
    colCount
}: UseDragAndCopyOptions<TContext>) {
    const [isDragging, setIsDragging] = useState(false);
    const [dragStartCell, setDragStartCell] = useState<DragCell | null>(null);
    const [dragEndCell, setDragEndCell] = useState<DragCell | null>(null);
    const [dragPreviewCells, setDragPreviewCells] = useState<DragCell[]>([]);

    useEffect(() => {

        if (dragStartCell && dragEndCell) {
            // Only allow vertical selection (same column)
            if (dragStartCell.colIndex === dragEndCell.colIndex) {
                const cells: DragCell[] = [];
                const startRow = Math.min(dragStartCell.rowIndex, dragEndCell.rowIndex);
                const endRow = Math.max(dragStartCell.rowIndex, dragEndCell.rowIndex);
                const col = dragStartCell.colIndex;
                for (let r = startRow; r <= endRow; r++) {
                    cells.push({ rowIndex: r, colIndex: col });
                }
                setDragPreviewCells(cells);
            } else {
                // If not vertical, only select the starting cell
                setDragPreviewCells([dragStartCell]);
            }
        }
    }, [dragStartCell, dragEndCell]);

    // Keyboard copy/paste logic (Ctrl+C/Ctrl+V)
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            // If selection exists, use it; otherwise, use activeCell for paste
            const hasSelection = dragPreviewCells && dragPreviewCells.length > 0;
            const hasActiveCell = !!activeCell;

            if (e.ctrlKey && (e.key === "c" || e.key === "C")) {
                if (!hasSelection) return;
                e.preventDefault();
                if (onCopy) {
                    const values = onCopy(dragPreviewCells, context!);
                    navigator.clipboard.writeText(values.join("\n"));
                }
            }

            if (e.ctrlKey && (e.key === "v" || e.key === "V")) {
                e.preventDefault();
                if (onPaste) {
                    navigator.clipboard.readText().then(text => {
                        const valuesToPaste = text
                            .trim()
                            .replace(/\r/g, "")
                            .split(/\r?\n|\t/);

                        let targetCells: DragCell[] = [];
                        // If only one cell is selected (after clicking), fill downward in the same column
                        if (
                            (hasSelection && dragPreviewCells.length === 1 && typeof rowCount === "number") ||
                            (!hasSelection && hasActiveCell && typeof rowCount === "number")
                        ) {
                            const start = hasSelection ? dragPreviewCells[0] : activeCell!;
                            const startRow = start.rowIndex;
                            const col = start.colIndex;
                            const maxRows = rowCount;
                            for (let i = 0; i < valuesToPaste.length && (startRow + i) < maxRows; i++) {
                                targetCells.push({ rowIndex: startRow + i, colIndex: col });
                            }
                        } else if (hasSelection) {
                            targetCells = dragPreviewCells;
                        }
                        if (targetCells.length > 0) {
                            onPaste(targetCells, valuesToPaste, context!);
                        }
                    });
                }
            }
        };

        document.addEventListener("keydown", handleKeyDown);
        return () => document.removeEventListener("keydown", handleKeyDown);
    }, [dragPreviewCells, onCopy, onPaste, context, activeCell, rowCount]);

    function handleDragStart(rowIndex: number, colIndex: number) {
        setIsDragging(true);
        const initial = { rowIndex, colIndex };
        setDragStartCell(initial);
        setDragEndCell(initial);
        setDragPreviewCells([initial]);
    }

    function handleDragMove(rowIndex: number, colIndex: number) {
        if (!isDragging || !dragStartCell) return;
        setDragEndCell({ rowIndex, colIndex });
    }

    function handleDragEnd() {
        setIsDragging(false);
        setDragStartCell(null);
        setDragEndCell(null);
    }

    function isDragPreviewCell(rowIndex: number, colIndex: number) {
        return dragPreviewCells.some(cell => cell.rowIndex === rowIndex && cell.colIndex === colIndex);
    }

    function resetDragSelection() {
        setDragPreviewCells([]);
        setDragStartCell(null);
        setDragEndCell(null);
    }

    return {
        isDragging,
        dragStartCell,
        dragEndCell,
        dragPreviewCells,
        handleDragStart,
        handleDragMove,
        handleDragEnd,
        isDragPreviewCell,
        resetDragSelection,
    };
}
