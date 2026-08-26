export const maintenanceKeys = {
  all: ['maintenance'] as const,
  lists: () => [...maintenanceKeys.all, 'list'] as const,
  list: (filters: Record<string, any>) => [...maintenanceKeys.lists(), { filters }] as const,
  details: () => [...maintenanceKeys.all, 'detail'] as const,
  detail: (id: string) => [...maintenanceKeys.details(), id] as const,
  comments: (id: string) => [...maintenanceKeys.detail(id), 'comments'] as const,
  attachments: (id: string) => [...maintenanceKeys.detail(id), 'attachments'] as const,
  history: (id: string) => [...maintenanceKeys.detail(id), 'history'] as const,
};
