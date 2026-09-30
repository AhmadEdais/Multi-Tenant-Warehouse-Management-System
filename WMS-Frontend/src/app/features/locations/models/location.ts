export interface LocationNode {
  id: number;
  parentLocationId: number | null;
  locationType: string;
  name: string;
  barcode: string | null;
  maxWeightCapacityKg: number | null;
  level: number;
  children: LocationNode[];
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
