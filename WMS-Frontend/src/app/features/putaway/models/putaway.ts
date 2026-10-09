export interface PutawayQueueItem {
  productId: number;
  sku: string;
  productName: string;
  quantityOnHand: number;
}

export interface PutawayQueueParams {
  warehouseId: number;
  searchTerm: string;
  pageNumber: number;
  pageSize: number;
}

export interface PutawayDestination {
  locationId: number;
  quantity: number;
}

export interface PutawayRequest {
  warehouseId: number;
  productId: number;
  destinations: PutawayDestination[];
}
