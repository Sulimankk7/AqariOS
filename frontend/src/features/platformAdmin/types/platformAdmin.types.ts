export type RegistrationStatus = "Pending" | "Approved" | "Rejected";

export interface LandlordRegistrationListItemDto {
  registrationId: string;
  userName: string;
  email?: string | null;
  companyName: string;
  registrationDate: string;
  status: RegistrationStatus;
}

export interface LandlordRegistrationPageDto {
  items: LandlordRegistrationListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface LandlordRegistrationDetailDto extends LandlordRegistrationListItemDto {
  phone?: string | null;
  companyDisplayName: string;
  companyType: string;
  countryCode: string;
  reviewedAt?: string | null;
  rejectionReason?: string | null;
}

export interface LandlordRegistrationReviewResultDto {
  registrationId: string;
  status: RegistrationStatus;
  reviewedAt: string;
}

export interface RejectLandlordRegistrationRequest {
  reason: string;
}

export interface PlatformAdministratorDto {
  id: string;
  fullName: string;
  email: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreatePlatformAdministratorRequest {
  fullName: string;
  email: string;
  password: string;
}
