import {
    PaginationContent,
    PaginationItem,
    PaginationLink,
    PaginationNext,
    PaginationPrevious,
    Pagination as ShadCNPagination,
} from "@/components/ui/pagination";
import React from "react";

interface PaginationProps {
    totalItems: number;
    itemsPerPage: number;
    currentPage: number;
    onPageChange: (page: number) => void;
}

const Pagination: React.FC<PaginationProps> = ({ totalItems, itemsPerPage, currentPage, onPageChange }) => {
    const totalPages = Math.ceil(totalItems / itemsPerPage);
    const pageNumbers = [...Array(totalPages)].map((_, i) => i + 1);

    const renderPageNumbers = () => {
        const maxPageNumbersToShow = 5;
        const halfMaxPageNumbersToShow = Math.floor(maxPageNumbersToShow / 2);
        let startPage = Math.max(1, currentPage - halfMaxPageNumbersToShow);
        let endPage = Math.min(totalPages, currentPage + halfMaxPageNumbersToShow);

        if (currentPage <= halfMaxPageNumbersToShow) {
            endPage = Math.min(totalPages, maxPageNumbersToShow);
        } else if (currentPage + halfMaxPageNumbersToShow >= totalPages) {
            startPage = Math.max(1, totalPages - maxPageNumbersToShow + 1);
        }

        const pages = [];
        for (let i = startPage; i <= endPage; i++) {
            pages.push(i);
        }

        return (
            <>
                {startPage > 1 && (
                    <>
                        <PaginationItem>
                            <PaginationLink
                                href="#"
                                onClick={(e) => {
                                    e.preventDefault();
                                    onPageChange(1);
                                }}
                            >
                                1
                            </PaginationLink>
                        </PaginationItem>
                        {startPage > 2 && <span>...</span>}
                    </>
                )}
                {pages.map(page => (
                    <PaginationItem key={page}>
                        <PaginationLink
                            href="#"
                            onClick={(e) => {
                                e.preventDefault();
                                onPageChange(page);
                            }}
                            isActive={page === currentPage}
                        >
                            {page}
                        </PaginationLink>
                    </PaginationItem>
                ))}
                {endPage < totalPages && (
                    <>
                        {endPage < totalPages - 1 && <span>...</span>}
                        <PaginationItem>
                            <PaginationLink
                                href="#"
                                onClick={(e) => {
                                    e.preventDefault();
                                    onPageChange(totalPages);
                                }}
                            >
                                {totalPages}
                            </PaginationLink>
                        </PaginationItem>
                    </>
                )}
            </>
        );
    };

    return (
        <ShadCNPagination>
            <PaginationContent>
                <PaginationItem>
                    <PaginationPrevious
                        href="#"
                        onClick={(e) => {
                            e.preventDefault();
                            if (currentPage > 1) onPageChange(currentPage - 1);
                        }}
                    />
                </PaginationItem>
                {renderPageNumbers()}
                <PaginationItem>
                    <PaginationNext
                        href="#"
                        onClick={(e) => {
                            e.preventDefault();
                            if (currentPage < totalPages) onPageChange(currentPage + 1);
                        }}
                    />
                </PaginationItem>
            </PaginationContent>
        </ShadCNPagination>
    );
};

export default Pagination;
