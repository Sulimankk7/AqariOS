import React from 'react';
import { RotateCcw } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useBuildings } from '@/features/buildings/hooks/useBuildings';
import { useTranslation } from '@/shared/i18n';
import { DatePicker } from '@/shared/components/ui/DatePicker';
import { ExpenseCategory, type ExpenseFilterParams } from '../types/financials.types';

interface ExpenseFiltersProps {
  filters: ExpenseFilterParams;
  onChange: (filters: ExpenseFilterParams) => void;
}

export function ExpenseFilters({ filters, onChange }: ExpenseFiltersProps) {
  const { t } = useTranslation();
  const { data: buildings, isLoading } = useBuildings();
  const update = (changes: Partial<ExpenseFilterParams>) => onChange({
    ...filters,
    ...changes,
    lastSeenId: null,
    lastSeenExpenseDate: null,
  });

  const hasFilters = Boolean(filters.buildingId || filters.category !== null && filters.category !== undefined || filters.dateFrom || filters.dateTo);

  return (
    <div className="bg-card border border-border rounded-lg p-4 space-y-3">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <select
          value={filters.buildingId || 'all'}
          onChange={(event) => update({ buildingId: event.target.value === 'all' ? null : event.target.value })}
          disabled={isLoading}
          aria-label={t('financials.building')}
          className="w-full h-9 ps-3 pe-8 text-xs rounded-lg border border-border bg-card text-foreground focus:outline-none focus:ring-1 focus:ring-ring cursor-pointer"
        >
          <option value="all">{t('financials.allBuildings')}</option>
          {buildings?.map((building) => <option key={building.id} value={building.id}>{building.name}</option>)}
        </select>

        <select
          value={filters.category !== null && filters.category !== undefined ? String(filters.category) : 'all'}
          onChange={(event) => update({ category: event.target.value === 'all' ? null : Number(event.target.value) })}
          aria-label={t('financials.expenseCategory')}
          className="w-full h-9 ps-3 pe-8 text-xs rounded-lg border border-border bg-card text-foreground focus:outline-none focus:ring-1 focus:ring-ring cursor-pointer"
        >
          <option value="all">{t('financials.allExpenseCategories')}</option>
          {Object.values(ExpenseCategory).filter((value): value is number => typeof value === 'number').map((value) => (
            <option key={value} value={value}>{t(`financials.expenseCategory${ExpenseCategory[value]}`)}</option>
          ))}
        </select>

        <DatePicker value={filters.dateFrom} onValueChange={(dateFrom) => update({ dateFrom })} ariaLabel={t('financials.expenseDateFrom')} className="h-9 text-xs" />
        <DatePicker value={filters.dateTo} onValueChange={(dateTo) => update({ dateTo })} ariaLabel={t('financials.expenseDateTo')} className="h-9 text-xs" />
      </div>
      {hasFilters && (
        <div className="flex justify-end">
          <Button variant="ghost" size="sm" onClick={() => onChange({ pageSize: filters.pageSize || 50 })} className="h-7 text-xs gap-1">
            <RotateCcw className="w-3.5 h-3.5" />{t('common.clear')}
          </Button>
        </div>
      )}
    </div>
  );
}
