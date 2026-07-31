export const buildingKeys = {
  all: ['buildings'] as const,
  lists: () => [...buildingKeys.all, 'list'] as const,
  list: (filters: string) => [...buildingKeys.lists(), { filters }] as const,
  details: () => [...buildingKeys.all, 'detail'] as const,
  detail: (id: string) => [...buildingKeys.details(), id] as const,
};
