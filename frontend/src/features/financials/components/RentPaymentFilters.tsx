import React, { useState, useEffect, useRef } from 'react';
import { SearchBar } from '@/shared/components/ui/Filters';
import { useBuildings } from '@/features/buildings/hooks/useBuildings';
import { useTranslation } from '@/shared/i18n';
import { DueDateStatus, type RentPaymentFilterParams } from '../types/financials.types';
import { RotateCcw } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { DatePicker } from '@/shared/components/ui/DatePicker';

interface RentPaymentFiltersProps {
  filters: RentPaymentFilterParams;
  onFiltersChange: (newFilters: RentPaymentFilterParams) => void;
}

export function RentPaymentFilters({ filters, onFiltersChange }: RentPaymentFiltersProps) {
  const { t } = useTranslation();
  const { data: buildings, isLoading: isLoadingBuildings } = useBuildings();

  const [localSearch, setLocalSearch] = useState(filters.searchTerm || '');
  const filtersRef = useRef(filters);
  const onFiltersChangeRef = useRef(onFiltersChange);

  useEffect(() => {
    filtersRef.current = filters;
    onFiltersChangeRef.current = onFiltersChange;
  }, [filters, onFiltersChange]);

  useEffect(() => {
    const externalSearch = filters.searchTerm || '';
    setLocalSearch((current) => current === externalSearch ? current : externalSearch);
  }, [filters.searchTerm]);

  // Debounced search effect
  useEffect(() => {
    const handler = setTimeout(() => {
      const currentFilters = filtersRef.current;
      if ((currentFilters.searchTerm || '') !== localSearch) {
        onFiltersChangeRef.current({
          ...currentFilters,
          searchTerm: localSearch ? localSearch.trim() : null,
          lastSeenId: null,
          lastSeenDueDate: null,
        });
      }
    }, 350);

    return () => clearTimeout(handler);
  }, [localSearch]);

  const handleBuildingChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const val = e.target.value;
    onFiltersChange({
      ...filters,
      buildingId: val === 'all' ? null : val,
      lastSeenId: null,
      lastSeenDueDate: null,
    });
  };

  const handleStatusChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const val = e.target.value;
    onFiltersChange({
      ...filters,
      status: val === 'all' ? null : Number(val),
      lastSeenId: null,
      lastSeenDueDate: null,
    });
  };

  const handleDateFromChange = (val: string | null) => {
    onFiltersChange({
      ...filters,
      dateFrom: val,
      lastSeenId: null,
      lastSeenDueDate: null,
    });
  };

  const handleDateToChange = (val: string | null) => {
    onFiltersChange({
      ...filters,
      dateTo: val,
      lastSeenId: null,
      lastSeenDueDate: null,
    });
  };

  const handleReset = () => {
    setLocalSearch('');
    onFiltersChange({
      buildingId: null,
      status: null,
      dateFrom: null,
      dateTo: null,
      searchTerm: null,
      lastSeenId: null,
      lastSeenDueDate: null,
      pageSize: filters.pageSize || 50,
    });
  };

  const hasActiveFilters = Boolean(
    filters.buildingId ||
    filters.status !== null && filters.status !== undefined ||
    filters.dateFrom ||
    filters.dateTo ||
    localSearch
  );

  const quickPresets = [
    { id: 'all', label: t('financials.filterAll'), status: null },
    { id: 'needsVerification', label: t('financials.filterNeedsVerification'), status: DueDateStatus.PendingVerification },
    { id: 'overdue', label: t('financials.filterOverdue'), status: DueDateStatus.OverdueUnpaid },
    { id: 'partiallyPaid', label: t('financials.filterPartiallyPaid'), status: DueDateStatus.PartiallyPaid },
  ];

  return (
    <div className="flex flex-col gap-3 bg-card p-3 rounded-lg border border-border">
      {/* Quick Operational Presets */}
      <div className="flex items-center gap-2 overflow-x-auto pb-0.5 scrollbar-none">
        <span className="text-xs font-semibold text-muted-foreground shrink-0 me-1">
          {t('financials.quickFilters')}
        </span>
        {quickPresets.map((preset) => {
          const isActive = preset.status === null
            ? (filters.status === null || filters.status === undefined)
            : filters.status === preset.status;
          return (
            <button
              key={preset.id}
              type="button"
              onClick={() => {
                onFiltersChange({
                  ...filters,
                  status: preset.status,
                  lastSeenId: null,
                  lastSeenDueDate: null,
                });
              }}
              className={`px-3 py-1 text-xs font-medium rounded-full border transition-all cursor-pointer whitespace-nowrap focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
                isActive
                  ? 'bg-primary text-primary-foreground border-primary shadow-2xs'
                  : 'bg-secondary/60 text-muted-foreground border-border hover:bg-secondary hover:text-foreground'
              }`}
            >
              {preset.label}
            </button>
          );
        })}
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {/* Search */}
        <SearchBar
          value={localSearch}
          onChange={setLocalSearch}
          placeholder={t('financials.searchPlaceholder') || 'بحث بالمستأجر، العقد، السند...'}
          className="max-w-full"
        />

        {/* Building Select */}
        <div className="relative">
          <select
            value={filters.buildingId || 'all'}
            onChange={handleBuildingChange}
            disabled={isLoadingBuildings}
            aria-label={t('financials.building')}
            className="w-full h-9 ps-3 pe-8 text-xs rounded-lg border border-border bg-card text-foreground focus:outline-none focus:ring-1 focus:ring-ring focus:border-border-strong cursor-pointer"
          >
            <option value="all">{t('financials.allBuildings')}</option>
            {buildings?.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </select>
        </div>

        {/* Status Select */}
        <div className="relative">
          <select
            value={filters.status !== null && filters.status !== undefined ? String(filters.status) : 'all'}
            onChange={handleStatusChange}
            aria-label={t('financials.status')}
            className="w-full h-9 ps-3 pe-8 text-xs rounded-lg border border-border bg-card text-foreground focus:outline-none focus:ring-1 focus:ring-ring focus:border-border-strong cursor-pointer"
          >
            <option value="all">{t('financials.allStatuses')}</option>
            <option value={String(DueDateStatus.Pending)}>{t('financials.statusPending')}</option>
            <option value={String(DueDateStatus.PartiallyPaid)}>{t('financials.statusPartiallyPaid')}</option>
            <option value={String(DueDateStatus.Late)}>{t('financials.statusLate')}</option>
            <option value={String(DueDateStatus.OverdueUnpaid)}>{t('financials.statusOverdue')}</option>
            <option value={String(DueDateStatus.Paid)}>{t('financials.statusPaid')}</option>
            <option value={String(DueDateStatus.PendingVerification)}>{t('financials.statusPendingVerification')}</option>
            <option value={String(DueDateStatus.Cancelled)}>{t('financials.statusCancelled')}</option>
          </select>
        </div>

        {/* Date Range: From / To */}
        <div className="flex items-center gap-1.5">
          <DatePicker value={filters.dateFrom} onValueChange={handleDateFromChange} ariaLabel={t('financials.dateFrom')} className="h-9 w-1/2 text-xs" />
          <span className="text-xs text-muted-foreground">-</span>
          <DatePicker value={filters.dateTo} onValueChange={handleDateToChange} ariaLabel={t('financials.dateTo')} className="h-9 w-1/2 text-xs" />
        </div>
      </div>

      {hasActiveFilters && (
        <div className="flex items-center justify-end pt-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={handleReset}
            className="text-xs text-muted-foreground hover:text-foreground gap-1 h-7 px-2"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            <span>{t('common.clear') || 'مسح التصفية'}</span>
          </Button>
        </div>
      )}
    </div>
  );
}
