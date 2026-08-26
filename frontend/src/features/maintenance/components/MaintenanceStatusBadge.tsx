import { StatusBadge, type StatusVariant } from '@/shared/components/ui/StatusBadge';
import { useTranslation } from '@/shared/i18n';
import { MaintenanceCategory, MaintenancePriority, MaintenanceStatus } from '../types/maintenance.types';

export const MaintenanceStatusBadge = ({ status }: { status: MaintenanceStatus }) => {
  const { t } = useTranslation();
  const values: Record<MaintenanceStatus, [string, StatusVariant]> = {
    [MaintenanceStatus.Open]: [t('maintenance.statuses.open'), 'info'], [MaintenanceStatus.InProgress]: [t('maintenance.statuses.inProgress'), 'warning'],
    [MaintenanceStatus.Waiting]: [t('maintenance.statuses.waiting'), 'neutral'], [MaintenanceStatus.Resolved]: [t('maintenance.statuses.resolved'), 'success'],
    [MaintenanceStatus.Closed]: [t('maintenance.statuses.closed'), 'neutral'], [MaintenanceStatus.Cancelled]: [t('maintenance.statuses.cancelled'), 'danger'],
  };
  const [label, variant] = values[status] || [String(status), 'neutral'];
  return <StatusBadge label={label} variant={variant} size="sm" />;
};

export const MaintenancePriorityBadge = ({ priority }: { priority: MaintenancePriority }) => {
  const { t } = useTranslation();
  const values: Record<MaintenancePriority, [string, StatusVariant]> = {
    [MaintenancePriority.Low]: [t('maintenance.priorities.low'), 'neutral'], [MaintenancePriority.Medium]: [t('maintenance.priorities.medium'), 'info'],
    [MaintenancePriority.High]: [t('maintenance.priorities.high'), 'warning'], [MaintenancePriority.Emergency]: [t('maintenance.priorities.emergency'), 'danger'],
  };
  const [label, variant] = values[priority] || [String(priority), 'neutral'];
  return <StatusBadge label={label} variant={variant} size="sm" />;
};

export function maintenanceCategoryLabel(category: MaintenanceCategory, t: (key: string) => string) {
  const keys: Record<MaintenanceCategory, string> = {
    [MaintenanceCategory.Electrical]: 'electrical', [MaintenanceCategory.Plumbing]: 'plumbing', [MaintenanceCategory.AirConditioning]: 'airConditioning',
    [MaintenanceCategory.Elevator]: 'elevator', [MaintenanceCategory.Cleaning]: 'cleaning', [MaintenanceCategory.Water]: 'water',
    [MaintenanceCategory.Structural]: 'structural', [MaintenanceCategory.DoorsWindows]: 'doorsWindows', [MaintenanceCategory.Internet]: 'internet', [MaintenanceCategory.Other]: 'other',
  };
  return t(`maintenance.categories.${keys[category]}`);
}
