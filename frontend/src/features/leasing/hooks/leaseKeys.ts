export const leaseKeys = {
  all: ['leases'] as const,
  lists: () => [...leaseKeys.all, 'list'] as const,
  search: (searchTerm?: string, pageSize?: number) => [...leaseKeys.lists(), 'search', { searchTerm, pageSize }] as const,
  expiring: (daysAhead?: number) => [...leaseKeys.lists(), 'expiring', { daysAhead }] as const,
  apartmentHistory: (apartmentId: string, pageSize?: number) => [...leaseKeys.lists(), 'history', 'apartment', apartmentId, { pageSize }] as const,
  details: () => [...leaseKeys.all, 'detail'] as const,
  detail: (id: string) => [...leaseKeys.details(), id] as const,
  tenants: (searchTerm?: string) => [...leaseKeys.all, 'tenants', { searchTerm }] as const,
};
