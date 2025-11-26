import { useCallback, useEffect, useState } from 'react';

export interface SpreadsheetNavigationConfig {
  tableRef: React.RefObject<HTMLElement>;
  rowCountCallback: () => number;
  columnCountCallback: () => number;
  isCellNavigableCallback?: (rowIndex: number, colIndex: number) => boolean;
  isCellEditableCallback: (rowIndex: number, colIndex: number) => boolean;
  onCellActivate: (rowIndex: number, colIndex: number, cellElement: HTMLElement | null) => void;
  onCellEditRequest: (rowIndex: number, colIndex: number) => void;
  onCellSubmit?: (rowIndex: number, colIndex: number) => void;
  onCellCancel?: (rowIndex: number, colIndex: number) => void;
  initialActiveCell?: { rowIndex: number; colIndex: number };
}

export interface ActiveCell {
  rowIndex: number;
  colIndex: number;
}

export interface SpreadsheetNavigationReturn {
  activeCell: ActiveCell | null;
  isEditing: boolean;
  tableProps: {
    onKeyDown: (event: React.KeyboardEvent) => void;
    tabIndex: number;
    role: string;
  };
  getCellProps: (rowIndex: number, colIndex: number) => {
    id: string;
    tabIndex: number;
    role: string;
    'aria-selected': boolean;
    'aria-readonly': boolean;
    onClick: () => void;
  };
}

export function useSpreadsheetNavigation(config: SpreadsheetNavigationConfig): SpreadsheetNavigationReturn {
  const {
    tableRef,
    rowCountCallback,
    columnCountCallback,
    isCellNavigableCallback = () => true,
    isCellEditableCallback,
    onCellActivate,
    onCellEditRequest,
    onCellSubmit,
    onCellCancel,
    initialActiveCell
  } = config;

  const [activeCell, setActiveCell] = useState<ActiveCell | null>(initialActiveCell || null);
  const [isEditing, setIsEditing] = useState(true);

  // Ensure focus on table container when active cell changes
  useEffect(() => {
    if (activeCell && tableRef.current && !isEditing) {
      tableRef.current.focus({ preventScroll: true });
    }
  }, [activeCell, tableRef, isEditing]);

  // Call onCellActivate when active cell changes
  useEffect(() => {
    if (activeCell) {
      const cellElement = getCellElement(activeCell.rowIndex, activeCell.colIndex);
      onCellActivate(activeCell.rowIndex, activeCell.colIndex, cellElement);
    }
  }, [activeCell, onCellActivate]);

  const getCellElement = useCallback((rowIndex: number, colIndex: number): HTMLElement | null => {
    if (!tableRef.current) return null;

    // Try to find the cell element using the generated ID
    const cellId = `cell-${rowIndex}-${colIndex}`;
    return tableRef.current.querySelector(`#${cellId}`) as HTMLElement;
  }, [tableRef]);

  const isValidCell = useCallback((rowIndex: number, colIndex: number): boolean => {
    const rowCount = rowCountCallback();
    const colCount = columnCountCallback();

    return (
      rowIndex >= 0 &&
      rowIndex < rowCount &&
      colIndex >= 0 &&
      colIndex < colCount &&
      isCellNavigableCallback(rowIndex, colIndex)
    );
  }, [rowCountCallback, columnCountCallback, isCellNavigableCallback]);

  const findNextNavigableCell = useCallback((
    startRow: number,
    startCol: number,
    direction: 'next' | 'previous'
  ): ActiveCell | null => {
    const rowCount = rowCountCallback();
    const colCount = columnCountCallback();

    let currentRow = startRow;
    let currentCol = startCol;

    if (direction === 'next') {
      // Move to next cell
      currentCol++;
      if (currentCol >= colCount) {
        currentCol = 0;
        currentRow++;
        if (currentRow >= rowCount) {
          return null; // End of table
        }
      }
    } else {
      // Move to previous cell
      currentCol--;
      if (currentCol < 0) {
        currentRow--;
        if (currentRow < 0) {
          return null; // Beginning of table
        }
        currentCol = colCount - 1;
      }
    }

    // Find the next navigable cell
    while (currentRow >= 0 && currentRow < rowCount) {
      while (currentCol >= 0 && currentCol < colCount) {
        if (isCellNavigableCallback(currentRow, currentCol)) {
          return { rowIndex: currentRow, colIndex: currentCol };
        }

        if (direction === 'next') {
          currentCol++;
        } else {
          currentCol--;
        }
      }

      if (direction === 'next') {
        currentRow++;
        currentCol = 0;
      } else {
        currentRow--;
        currentCol = colCount - 1;
      }
    }

    return null;
  }, [rowCountCallback, columnCountCallback, isCellNavigableCallback]);

  const moveActiveCell = useCallback((direction: 'up' | 'down' | 'left' | 'right'): void => {
    if (!activeCell) return;

    let newRow = activeCell.rowIndex;
    let newCol = activeCell.colIndex;

    switch (direction) {
      case 'up':
        newRow = Math.max(0, newRow - 1);
        break;
      case 'down':
        newRow = Math.min(rowCountCallback() - 1, newRow + 1);
        break;
      case 'left':
        newCol = Math.max(0, newCol - 1);
        break;
      case 'right':
        newCol = Math.min(columnCountCallback() - 1, newCol + 1);
        break;
    }

    // Find the next navigable cell in the direction
    while (isValidCell(newRow, newCol) && !isCellNavigableCallback(newRow, newCol)) {
      switch (direction) {
        case 'up':
          newRow = Math.max(0, newRow - 1);
          break;
        case 'down':
          newRow = Math.min(rowCountCallback() - 1, newRow + 1);
          break;
        case 'left':
          newCol = Math.max(0, newCol - 1);
          break;
        case 'right':
          newCol = Math.min(columnCountCallback() - 1, newCol + 1);
          break;
      }
    }

    if (isValidCell(newRow, newCol)) {
      setActiveCell({ rowIndex: newRow, colIndex: newCol });
    }
  }, [activeCell, rowCountCallback, columnCountCallback, isValidCell, isCellNavigableCallback]);

  const handleKeyDown = useCallback((event: React.KeyboardEvent): void => {
    if (!activeCell) return;

    const { key, shiftKey } = event;

    // Handle navigation keys
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(key)) {
      event.preventDefault();


      switch (key) {
        case 'ArrowUp':
          moveActiveCell('up');
          break;
        case 'ArrowDown':
          moveActiveCell('down');
          break;
        case 'ArrowLeft':
          moveActiveCell('left');
          break;
        case 'ArrowRight':
          moveActiveCell('right');
          break;
      }
    }

    // Handle Tab navigation
    else if (key === 'Tab') {
      event.preventDefault();
      setIsEditing(true);



      const nextCell = findNextNavigableCell(
        activeCell.rowIndex,
        activeCell.colIndex,
        shiftKey ? 'previous' : 'next'
      );

      if (nextCell) {
        const cellElement = getCellElement(activeCell.rowIndex, activeCell.colIndex,);
        cellElement?.classList.remove('border-2', 'border-blue-500');
        setActiveCell(nextCell);
      } else {
        // Allow browser default tab behavior when reaching table boundaries
        const nextRow = activeCell.rowIndex + 1;
        if (isValidCell(nextRow, activeCell.colIndex)) {
          setActiveCell({ rowIndex: nextRow, colIndex: activeCell.colIndex });
        }
      }
    }

    // Handle Enter key
    else if (key === 'Enter') {
      event.preventDefault();

      // If editing, submit edit and exit edit mode
      if (onCellSubmit) {
        onCellSubmit(activeCell.rowIndex, activeCell.colIndex);
      }
      setIsEditing(true);


      // Always move to the cell below if possible
      const nextRow = activeCell.rowIndex + 1;
      if (isValidCell(nextRow, activeCell.colIndex)) {
        setActiveCell({ rowIndex: nextRow, colIndex: activeCell.colIndex });
      }
      // If at the last row, stay in the current cell (no wrap)
    }

    // Handle Escape key
    else if (key === 'Escape') {
      if (isEditing) {
        event.preventDefault();
        if (onCellCancel) {
          onCellCancel(activeCell.rowIndex, activeCell.colIndex);
        }
      }
    }

    // Handle printable characters
    else if (
      !isEditing &&
      key.length === 1 &&
      !event.ctrlKey &&
      !event.metaKey &&
      !event.altKey &&
      isCellEditableCallback(activeCell.rowIndex, activeCell.colIndex)
    ) {
      event.preventDefault();
      // Start editing and pass the character
      setIsEditing(true);
      onCellEditRequest(activeCell.rowIndex, activeCell.colIndex);
    }

    // Handle other keys that might cause scrolling
    else if (['Space', 'PageUp', 'PageDown', 'Home', 'End'].includes(key)) {
      event.preventDefault();
      // Prevent default scrolling behavior for these keys
    }
  }, [
    activeCell,
    isEditing,
    moveActiveCell,
    findNextNavigableCell,
    isCellEditableCallback,
    onCellEditRequest,
    onCellSubmit,
    onCellCancel,
    tableRef,
    isValidCell
  ]);

  const getCellProps = useCallback((rowIndex: number, colIndex: number) => {
    const isActiveCell = activeCell?.rowIndex === rowIndex && activeCell?.colIndex === colIndex;
    const isEditable = isCellEditableCallback(rowIndex, colIndex);

    return {
      id: `cell-${rowIndex}-${colIndex}`,
      tabIndex: isActiveCell ? 0 : -1,
      role: 'gridcell',
      'aria-selected': isActiveCell,
      'aria-readonly': !isEditable,
      onClick: () => {
        setActiveCell({ rowIndex, colIndex });
        setIsEditing(true);
      }
    };
  }, [activeCell, isCellEditableCallback]);

  const tableProps = {
    onKeyDown: handleKeyDown,
    tabIndex: 0,
    role: 'grid'
  };

  return {
    activeCell,
    isEditing,
    tableProps,
    getCellProps
  };
}