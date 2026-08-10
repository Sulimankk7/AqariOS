export const tenantKeys = {
  all: ['tenants'] as const,
  lists: () => [...tenantKeys.all, 'list'] as const,
  search: (searchTerm?: string) => [...tenantKeys.lists(), 'search', { searchTerm }] as const,
  details: () => [...tenantKeys.all, 'detail'] as const,
  detail: (id: string) => [...tenantKeys.details(), id] as const,
  leases: (id: string, pageSize?: number) => [...tenantKeys.detail(id), 'leases', { pageSize }] as const,
  familyMembers: (id: string) => [...tenantKeys.detail(id), 'family-members'] as const,
  emergencyContacts: (id: string) => [...tenantKeys.detail(id), 'emergency-contacts'] as const,
  vehicles: (id: string) => [...tenantKeys.detail(id), 'vehicles'] as const,
};
