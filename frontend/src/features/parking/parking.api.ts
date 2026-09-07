import { http } from "@/shared/lib/http";
import { z } from "zod";
export const parkingSchema = z.object({
  spotCode: z.string().trim().min(1).max(50),
  parkingType: z.number().int().min(0).max(3),
  defaultApartmentId: z.string().uuid().nullable(),
  locationDescription: z.string().max(255).nullable(),
});
export type ParkingInput = z.infer<typeof parkingSchema>;
export interface ParkingSpot extends ParkingInput {
  id: string;
  companyId: string;
  buildingId: string;
  isActive: boolean;
}
export interface ParkingAssignment {
  assignmentId: string;
  parkingSpotId: string;
  parkingSpotCode: string;
  parkingType: number | string;
  location: string | null;
  leaseContractId: string;
  tenantId: string;
  tenantName: string;
  startDate: string;
  endDate: string | null;
  status: number | string;
}
export const parkingApi = {
  list: (buildingId: string) =>
    http.get<ParkingSpot[]>(`/api/v1/buildings/${buildingId}/parking-spots`),
  create: (buildingId: string, body: ParkingInput) =>
    http.post<void>(`/api/v1/buildings/${buildingId}/parking-spots`, body),
  update: (id: string, body: ParkingInput) =>
    http.put<void>(`/api/v1/parking-spots/${id}`, body),
  archive: (id: string) => http.delete<void>(`/api/v1/parking-spots/${id}`),
  getCurrentAssignment: (parkingSpotId: string) =>
    http.get<ParkingAssignment | undefined>(`/api/v1/parking-spots/${parkingSpotId}/assignment`)
      .then((assignment) => assignment ?? null),
  getLeaseParking: (leaseContractId: string) =>
    http.get<ParkingAssignment[]>(`/api/v1/leasing/contracts/${leaseContractId}/parking`),
  assign: (parkingSpotId: string, leaseContractId: string) =>
    http.post<{ assignmentId: string }>(`/api/v1/parking-spots/${parkingSpotId}/assignment`, { leaseContractId }),
  endAssignment: (assignmentId: string) =>
    http.post<void>(`/api/v1/parking-assignments/${assignmentId}/end`),
};
