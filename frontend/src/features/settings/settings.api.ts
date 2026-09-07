import { http } from "@/shared/lib/http";
import { z } from "zod";
export const profileSchema = z.object({
  legalName: z.string().trim().min(1),
  displayName: z.string().trim().min(1),
  primaryPhone: z.string().trim().min(1),
  primaryEmail: z.union([z.string().email(), z.literal(""), z.null()]),
});
export const settingsSchema = z
  .object({
    rentGracePeriodDays: z.number().int().min(0).max(32767),
    lateFeeType: z.number().int().min(0).max(2),
    lateFeeValue: z.number().nullable(),
    fiscalYearStartMonth: z.number().int().min(1).max(12),
  })
  .refine((v) =>
    v.lateFeeType === 0
      ? v.lateFeeValue === null
      : v.lateFeeValue !== null && v.lateFeeValue > 0,
  );
export type CompanyProfile = z.infer<typeof profileSchema>;
export type OperationalInput = z.infer<typeof settingsSchema>;
export interface Company extends CompanyProfile {
  id: string;
  commercialRegistrationNo?: string;
  taxNumber?: string;
  companyType: number;
  countryCode: string;
  isActive: boolean;
}
export interface CompanySettings extends OperationalInput {
  defaultCurrency: string;
  defaultLanguage: string;
  timezone: string;
}
export const settingsApi = {
  company: () => http.get<Company>("/api/v1/companies/me"),
  updateProfile: (id: string, body: CompanyProfile) =>
    http.put<void>(`/api/v1/companies/${id}`, body),
  settings: (id: string) =>
    http.get<CompanySettings>(`/api/v1/companies/${id}/settings`),
  updateSettings: (id: string, body: OperationalInput) =>
    http.put<void>(`/api/v1/companies/${id}/settings`, body),
  logoutAll: () => http.post<void>("/api/v1/auth/logout-all", {}),
};
