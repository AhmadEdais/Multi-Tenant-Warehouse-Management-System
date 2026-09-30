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
