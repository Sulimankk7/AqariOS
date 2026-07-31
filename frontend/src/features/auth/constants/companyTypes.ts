/**
 * Auth feature — company type options.
 * Maps UI label strings to C# enum values (CompanyType).
 */

import { CompanyType } from "@/features/auth/types/auth.types";

export interface CompanyTypeOption {
  label: string;
  value: CompanyType;
}

export const COMPANY_TYPES: CompanyTypeOption[] = [
  { label: "Individual Owner", value: CompanyType.IndividualOwner },
  { label: "Property Management Company", value: CompanyType.PropertyManagementCompany },
  { label: "Investment Company", value: CompanyType.InvestmentCompany },
];
