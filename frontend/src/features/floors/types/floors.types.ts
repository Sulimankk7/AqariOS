import { FloorType } from '../constants/floorEnums';

export { FloorType };

export interface FloorDto {
  id: string;
  companyId: string;
  buildingId: string;
  floorNumber: number;
  floorLabel: string;
  floorType: FloorType;
  apartmentsCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateFloorRequest {
  floorNumber: number;
  floorLabel: string;
  floorType: FloorType;
}

export interface UpdateFloorRequest {
  floorLabel: string;
  floorType: FloorType;
}
