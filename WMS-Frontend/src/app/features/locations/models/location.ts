export type LocationType = 'Zone' | 'Aisle' | 'Rack' | 'Bin';

export interface LocationNode {
  id: number;
  parentLocationId: number | null;
  locationType: string;
  name: string;
  barcode: string | null;
  maxWeightCapacityKg: number | null;
  isActive: boolean;
  level: number;
  children: LocationNode[];
}

export interface CreateLocationRequest {
  warehouseId: number;
  parentLocationId: number | null;
  locationType: LocationType;
  name: string;
  barcode: string | null;
  maxWeightCapacityKg: number | null;
}

export interface UpdateLocationRequest {
  parentLocationId: number | null;
  name: string;
  barcode: string | null;
  maxWeightCapacityKg: number | null;
}

export interface LocationDetails {
  id: number;
  warehouseId: number;
  warehouseName: string;
  warehouseCode: string;
  parentLocationId: number | null;
  parentLocationName: string | null;
  locationType: string;
  name: string;
  barcode: string | null;
  maxWeightCapacityKg: number | null;
  isActive: boolean;
}
