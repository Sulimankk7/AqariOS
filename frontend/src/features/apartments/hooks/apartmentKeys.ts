import { ListApartmentsParams } from '../types/apartments.types';

export const apartmentKeys = {
  all: ['apartments'] as const,
  lists: () => [...apartmentKeys.all, 'list'] as const,
  list: (params?: ListApartmentsParams) => [...apartmentKeys.lists(), { ...params }] as const,
  details: () => [...apartmentKeys.all, 'detail'] as const,
  detail: (id: string) => [...apartmentKeys.details(), id] as const,
};
