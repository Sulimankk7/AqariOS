import { useAuth } from '@/features/auth/hooks/useAuth';
import { useSearchTenants } from '@/features/tenants/hooks/useTenants';
import { useTranslation } from '@/shared/i18n';

export function useMaintenanceActorName() {
  const { user } = useAuth();
  const { data: tenants } = useSearchTenants('');
  const { t } = useTranslation();

  return (userId?: string | null) => {
    if (!userId) return t('maintenance.systemUser');
    if (user?.id === userId) return user.name;
    const tenant = tenants?.find((item) => item.userId === userId);
    return tenant?.name || t('maintenance.systemUser');
  };
}
