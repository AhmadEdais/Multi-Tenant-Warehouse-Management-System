export interface TenantStockSummary {
  productId: number;
  sku: string;
  productName: string;
  reorderPoint: number;
  totalQuantityOnHand: number;
  totalQuantityAllocated: number;
  totalAvailableQuantity: number;
  isLowStock: boolean;
}

export interface StockByProduct {
  locationId: number;
  locationName: string;
  locationType: string;
  warehouseId: number;
  warehouseCode: string;
  warehouseName: string;
  quantityOnHand: number;
  quantityAllocated: number;
  availableQuantity: number;
}

export interface StockByLocation {
  productId: number;
  sku: string;
  productName: string;
  reorderPoint: number;
  quantityOnHand: number;
  quantityAllocated: number;
  availableQuantity: number;
  isLowStock: boolean;
}

export interface InventorySummaryParams {
  searchTerm: string;
  pageNumber: number;
  pageSize: number;
}
