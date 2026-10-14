export interface SalesOrderPickupLine {
  id: number;
  salesOrderAllocationId: number;
  salesOrderLineId: number;
  productId: number;
  productSku: string;
  productName: string;
  stockLevelId: number;
  locationId: number;
  locationName: string;
  locationBarcode: string | null;
  quantity: number;
  stockMovementId: number | null;
}

export interface SalesOrderPickup {
  id: number;
  salesOrderId: number;
  pickedOnUtc: string;
  pickedBy: string;
  pickedByFullName: string;
  receivedBy: string | null;
  notes: string | null;
  totalPickedQuantity: number;
  lines: SalesOrderPickupLine[];
}
