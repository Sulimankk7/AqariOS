import { http } from '@/shared/lib/http';

export interface ReverseGeocodingResult {
  countryCode?: string | null; country?: string | null; governorate?: string | null;
  city?: string | null; district?: string | null; neighborhood?: string | null;
  street?: string | null; houseNumber?: string | null; postalCode?: string | null;
  formattedAddress?: string | null; language: string;
}

export const locationsApi = {
  reverseGeocode(latitude: number, longitude: number, language: 'ar' | 'en', signal?: AbortSignal) {
    const query = new URLSearchParams({ latitude: String(latitude), longitude: String(longitude), language });
    return http.get<ReverseGeocodingResult>(`/api/v1/locations/reverse-geocode?${query}`, { signal });
  },
};
