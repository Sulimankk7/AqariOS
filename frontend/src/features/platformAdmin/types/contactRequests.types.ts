export type ContactRequestStatus = "New" | "Contacted" | "TrialStarted" | "Converted" | "Rejected";
export interface ContactRequestListItem { id: string; name: string; companyName: string; phoneNumber: string; numberOfBuildings: number; status: ContactRequestStatus; createdAt: string; }
export interface ContactRequestDetail extends ContactRequestListItem { notes?: string | null; updatedAt: string; }
export interface ContactRequestPage { items: ContactRequestListItem[]; page: number; pageSize: number; totalCount: number; }
