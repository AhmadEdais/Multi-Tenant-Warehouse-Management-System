export const VALID_LOCATION_TYPES = ['Dock', 'Zone', 'Aisle', 'Rack', 'Bin'] as const;
export type LocationType = (typeof VALID_LOCATION_TYPES)[number];

export const ADDABLE_LOCATION_TYPES = ['Zone', 'Aisle', 'Rack', 'Bin'] as const;
export type AddableLocationType = (typeof ADDABLE_LOCATION_TYPES)[number];

export const EDITABLE_LOCATION_TYPES: readonly AddableLocationType[] = ADDABLE_LOCATION_TYPES;

export function isValidLocationType(type: string): type is LocationType {
  return VALID_LOCATION_TYPES.some((validType) => validType === type);
}

export function isAddableLocationType(type: string): type is AddableLocationType {
  return ADDABLE_LOCATION_TYPES.some((addableType) => addableType === type);
}

export function isEditableLocationType(type: string): type is AddableLocationType {
  return EDITABLE_LOCATION_TYPES.some((editableType) => editableType === type);
}

export function isDockLocationType(type: string): type is 'Dock' {
  return type === 'Dock';
}

export function canHaveChildLocation(type: string): boolean {
  return type === 'Zone' || type === 'Aisle' || type === 'Rack';
}

export function canViewLocationInventory(type: string): boolean {
  return isDockLocationType(type) || type === 'Bin';
}

export interface LocationNode {
  id: number;
  parentLocationId: number | null;
  locationType: LocationType;
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
  locationType: AddableLocationType;
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
  locationType: LocationType;
  name: string;
  barcode: string | null;
  maxWeightCapacityKg: number | null;
  isActive: boolean;
}
