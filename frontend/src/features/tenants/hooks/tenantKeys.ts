export const tenantKeys = {
  all: ['tenants'] as const,
  lists: () => [...tenantKeys.all, 'list'] as const,
  search: (searchTerm?: string) => [...tenantKeys.lists(), 'search', { searchTerm }] as const,
  details: () => [...tenantKeys.all, 'detail'] as const,
  detail: (id: string) => [...tenantKeys.details(), id] as const,
  leases: (id: string, pageSize?: number) => [...tenantKeys.detail(id), 'leases', { pageSize }] as const,
};
