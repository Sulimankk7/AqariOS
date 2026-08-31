/**
 * DataTable Component — Enterprise Data Table for AqariOS.
 * Supports typed generic columns, custom cell formatters, sorting, search, pagination,
 * error handling, and server-driven dataset execution.
 *
 * Strictly presentational: Zero knowledge of TanStack Query, HTTP, DTOs, or domain models.
 */

import React, { useState, useMemo } from "react";
import { ArrowUpDown, ArrowUp, ArrowDown, AlertTriangle, RefreshCw } from "lucide-react";
import { SearchBar } from "./Filters";
import { Pagination } from "./NavigationUI";
import { EmptyState, Skeleton } from "./Feedback";
import { useTranslation } from "@/shared/i18n";

export interface Column<T> {
  key: string;
  header: string;
  accessor?: (row: T) => any;
  cell?: (row: T) => React.ReactNode;
  sortable?: boolean;
  align?: "start" | "center" | "end";
}

export interface DataTablePaginationConfig {
  page: number;
  pageSize: number;
  totalRecords: number;
  onPageChange: (page: number) => void;
}

export interface DataTableSearchConfig {
  searchTerm: string;
  placeholder?: string;
  debounceMs?: number;
  onChange: (searchTerm: string) => void;
}

export interface DataTableSortConfig {
  key?: string;
  direction?: "asc" | "desc";
  onChange: (key: string, direction: "asc" | "desc") => void;
}

export interface DataTableErrorConfig {
  isError: boolean;
  message?: string;
  onRetry?: () => void;
}

export interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  isLoading?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  onRowClick?: (row: T) => void;
  className?: string;

  // Backward-Compatible Flat Props (Used by client-side tables)
  searchable?: boolean;
  searchPlaceholder?: string;
  pageSize?: number;

  // Grouped Enterprise Configuration Objects (Optional Server-Driven Mode)
  paginationConfig?: DataTablePaginationConfig;
  searchConfig?: DataTableSearchConfig;
  sortConfig?: DataTableSortConfig;
  errorConfig?: DataTableErrorConfig;
}

export function DataTable<T extends Record<string, any>>({
  data,
  columns,
  isLoading = false,
  searchable = true,
  searchPlaceholder,
  pageSize = 10,
  emptyTitle,
  emptyDescription,
  onRowClick,
  className = "",
  paginationConfig,
  searchConfig,
  sortConfig,
  errorConfig,
}: DataTableProps<T>) {
  const { t } = useTranslation();

  // Internal state used strictly for client-side mode (when server config objects are omitted)
  const [searchQuery, setSearchQuery] = useState("");
  const [sortKey, setSortKey] = useState<string | undefined>(undefined);
  const [sortOrder, setSortOrder] = useState<"asc" | "desc">("asc");
  const [currentPage, setCurrentPage] = useState(1);

  // Active Sort Parameters (Server Config vs Internal Fallback)
  const activeSortKey = sortConfig ? sortConfig.key : sortKey;
  const activeSortOrder = sortConfig ? sortConfig.direction || "asc" : sortOrder;

  // 1. Filter Data (Skipped when searchConfig is provided as server handles search)
  const filteredData = useMemo(() => {
    if (searchConfig) return data;
    if (!searchQuery.trim()) return data;
    const query = searchQuery.toLowerCase();
    return data.filter((row) =>
      Object.values(row).some((val) =>
        String(val ?? "").toLowerCase().includes(query)
      )
    );
  }, [data, searchConfig, searchQuery]);

  // 2. Sort Data (Skipped when sortConfig is provided as server handles sorting)
  const sortedData = useMemo(() => {
    if (sortConfig) return filteredData;
    if (!sortKey) return filteredData;
    const col = columns.find((c) => c.key === sortKey);
    return [...filteredData].sort((a, b) => {
      const valA = col?.accessor ? col.accessor(a) : a[sortKey];
      const valB = col?.accessor ? col.accessor(b) : b[sortKey];

      if (valA < valB) return sortOrder === "asc" ? -1 : 1;
      if (valA > valB) return sortOrder === "asc" ? 1 : -1;
      return 0;
    });
  }, [filteredData, sortConfig, sortKey, sortOrder, columns]);

  // 3. Paginate Data (Skipped when paginationConfig is provided as server returns single page window)
  const isServerPagination = Boolean(paginationConfig);
  const activePage = paginationConfig ? paginationConfig.page : currentPage;
  const activePageSize = paginationConfig ? paginationConfig.pageSize : pageSize;
  const totalRecordCount = paginationConfig ? paginationConfig.totalRecords : sortedData.length;
  const totalPageCount = paginationConfig
    ? Math.ceil(paginationConfig.totalRecords / paginationConfig.pageSize) || 1
    : Math.ceil(sortedData.length / pageSize) || 1;

  const displayRows = useMemo(() => {
    if (isServerPagination) return data;
    const start = (activePage - 1) * activePageSize;
    return sortedData.slice(start, start + activePageSize);
  }, [data, isServerPagination, sortedData, activePage, activePageSize]);

  // Handle Sort Toggle Click
  const handleSort = (key: string) => {
    if (sortConfig) {
      const nextDir = sortConfig.key === key && sortConfig.direction === "asc" ? "desc" : "asc";
      sortConfig.onChange(key, nextDir);
    } else {
      if (sortKey === key) {
        if (sortOrder === "asc") {
          setSortOrder("desc");
        } else {
          setSortKey(undefined);
          setSortOrder("asc");
        }
      } else {
        setSortKey(key);
        setSortOrder("asc");
      }
    }
  };

  const alignClasses = {
    start: "text-start",
    center: "text-center",
    end: "text-end",
  };

  const showSearch = searchConfig !== undefined || searchable;

  return (
    <div className={`w-full space-y-4 ${className}`}>
      {/* Search Toolbar */}
      {showSearch && (
        <div className="flex items-center justify-between gap-4">
          <SearchBar
            value={searchConfig ? searchConfig.searchTerm : searchQuery}
            onChange={(val) => {
              if (searchConfig) {
                searchConfig.onChange(val);
              } else {
                setSearchQuery(val);
                setCurrentPage(1);
              }
            }}
            placeholder={searchConfig?.placeholder || searchPlaceholder}
          />
        </div>
      )}

      {/* Main Table Surface */}
      <div className="w-full rounded-lg border border-outline-variant bg-card shadow-e0">
        <div className="overflow-x-auto min-h-[350px]">
          <table className="w-full text-xs text-start border-collapse">
            <thead className="bg-surface-container-high border-b border-outline-variant text-on-surface-variant type-label-medium uppercase">
              <tr>
                {columns.map((col) => (
                  <th
                    key={col.key}
                    scope="col"
                    className={`px-4 py-3 ${alignClasses[col.align || "start"]}`}
                  >
                    {col.sortable ? (
                      <button
                        onClick={() => handleSort(col.key)}
                        className="inline-flex items-center gap-1.5 hover:text-foreground transition-colors cursor-pointer"
                      >
                        <span>{col.header}</span>
                        {activeSortKey === col.key ? (
                          activeSortOrder === "asc" ? (
                            <ArrowUp className="w-3.5 h-3.5 text-primary" />
                          ) : (
                            <ArrowDown className="w-3.5 h-3.5 text-primary" />
                          )
                        ) : (
                          <ArrowUpDown className="w-3 h-3 text-muted-foreground/60" />
                        )}
                      </button>
                    ) : (
                      <span>{col.header}</span>
                    )}
                  </th>
                ))}
              </tr>
            </thead>

            <tbody className="divide-y divide-outline-variant/80">
              {isLoading ? (
                Array.from({ length: 5 }).map((_, rIdx) => (
                  <tr key={rIdx}>
                    {columns.map((col) => (
                      <td key={col.key} className="px-4 py-3.5">
                        <Skeleton className="h-4 w-full max-w-32" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : errorConfig?.isError ? (
                <tr>
                  <td colSpan={columns.length} className="px-4 py-8 text-center">
                    <div className="flex flex-col items-center justify-center space-y-3">
                      <div className="flex items-center gap-2 text-danger">
                        <AlertTriangle className="w-5 h-5" />
                        <span className="font-semibold text-sm">
                          {errorConfig.message || t("errors.generic")}
                        </span>
                      </div>
                      {errorConfig.onRetry && (
                        <button
                          type="button"
                          onClick={errorConfig.onRetry}
                          className="inline-flex items-center gap-2 px-3 py-1.5 rounded-md text-xs font-semibold bg-background border border-border hover:bg-muted transition-colors text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        >
                          <RefreshCw className="w-3.5 h-3.5" />
                          {t("common.retry")}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ) : displayRows.length === 0 ? (
                <tr>
                  <td colSpan={columns.length} className="px-4 py-8">
                    <EmptyState title={emptyTitle} description={emptyDescription} />
                  </td>
                </tr>
              ) : (
                displayRows.map((row, rIdx) => (
                  <tr
                    key={rIdx}
                    onClick={() => onRowClick && onRowClick(row)}
                    className={`transition-colors ${
                      onRowClick ? "hover:bg-surface-container-low cursor-pointer" : "hover:bg-surface-container-low/70"
                    }`}
                  >
                    {columns.map((col) => (
                      <td
                        key={col.key}
                        className={`px-4 py-3.5 text-foreground ${alignClasses[col.align || "start"]}`}
                      >
                        {col.cell
                          ? col.cell(row)
                          : col.accessor
                          ? col.accessor(row)
                          : row[col.key]}
                      </td>
                    ))}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination Controls */}
      {!isLoading && !errorConfig?.isError && totalRecordCount > 0 && (
        <Pagination
          currentPage={activePage}
          totalPages={totalPageCount}
          onPageChange={(p) => {
            if (paginationConfig) {
              paginationConfig.onPageChange(p);
            } else {
              setCurrentPage(p);
            }
          }}
          totalRecords={totalRecordCount}
          pageSize={activePageSize}
        />
      )}
    </div>
  );
}
