import { http } from "@/shared/lib/http";

export interface CreateContactRequestPayload {
  name: string;
  companyName: string;
  phoneNumber: string;
  numberOfBuildings: number;
  notes?: string;
}

export interface ContactRequestCreated {
  id: string;
  status: string;
  createdAt: string;
}

export const contactRequestsApi = {
  create(payload: CreateContactRequestPayload) {
    return http.post<ContactRequestCreated>("/api/v1/contact-requests", payload, { skipAuth: true });
  },
};
