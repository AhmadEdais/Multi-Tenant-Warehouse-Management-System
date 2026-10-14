export type SalesOrderStatus = 'Draft' | 'Allocated' | 'Picked' | 'Shipped' | 'Canceled';

export interface SalesOrderListItem {
  id: number;
  orderNumber: string;
  status: SalesOrderStatus;
  customerId: number;
  customerCode: string;
  customerName: string;
  warehouseId: number;
  warehouseCode: string;
  warehouseName: string;
  requestedShipmentDate: string | null;
  createdOnUtc: string;
  totalAmount: number;
  totalOrderedQuantity: number;
  totalAllocatedQuantity: number;
  totalPickedQuantity: number;
  totalRemainingQuantity: number;
}

export interface ListSalesOrdersParams {
  search: string;
  status: SalesOrderStatus | null;
  customerId: number | null;
  warehouseId: number | null;
  pageNumber: number;
  pageSize: number;
}

export interface CreateSalesOrderLineRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface CreateSalesOrderRequest {
  customerId: number;
  warehouseId: number;
  requestedShipmentDate: string | null;
  lines: CreateSalesOrderLineRequest[];
}

export type UpdateSalesOrderRequest = CreateSalesOrderRequest;

export interface SalesOrderAvailabilityRequest {
  warehouseId: number;
  lines: { productId: number; quantity: number }[];
}

export interface SalesOrderLineAvailability {
  productId: number;
  requestedQuantity: number;
  availableQuantity: number;
  shortageQuantity: number;
  hasSufficientStock: boolean;
}

export interface SalesOrderAvailabilityResult {
  warehouseId: number;
  allLinesSufficient: boolean;
  lines: SalesOrderLineAvailability[];
}

export type SalesOrderAllocationStatus = 'Active' | 'Released' | 'Shipped';

export interface SalesOrderAllocationDetails {
  id: number;
  stockLevelId: number;
  locationId: number;
  locationName: string;
  locationBarcode: string | null;
  quantity: number;
  quantityPicked: number;
  remainingQuantity: number;
  status: SalesOrderAllocationStatus;
  createdBy: string;
  createdOnUtc: string;
  releasedBy: string | null;
  releasedOnUtc: string | null;
  releaseReason: string | null;
}

export interface SalesOrderLineDetails {
  id: number;
  productId: number;
  productSku: string;
  productName: string;
  quantity: number;
  quantityAllocated: number;
  quantityPicked: number;
  remainingQuantity: number;
  unitPrice: number;
  lineTotal: number;
  allocations: SalesOrderAllocationDetails[];
}

export interface SalesOrderDetails {
  id: number;
  orderNumber: string;
  status: SalesOrderStatus;
  customerId: number;
  customerCode: string;
  customerName: string;
  warehouseId: number;
  warehouseCode: string;
  warehouseName: string;
  requestedShipmentDate: string | null;
  createdOnUtc: string;
  createdBy: string;
  createdByFullName: string;
  lastModifiedOnUtc: string | null;
  lastModifiedBy: string | null;
  lastModifiedByFullName: string | null;
  rowVersion: string;
  totalAmount: number;
  totalOrderedQuantity: number;
  totalPickedQuantity: number;
  totalRemainingQuantity: number;
  totalAllocatedQuantity: number;
  lines: SalesOrderLineDetails[];
}

export interface CreateSalesOrderPickupRequest {
  receivedBy: string | null;
  notes: string | null;
  lines: { salesOrderAllocationId: number; quantity: number }[];
}

export interface SalesOrderPickupResult {
  pickupId: number;
  salesOrderId: number;
  salesOrderStatus: SalesOrderStatus;
  totalPickedQuantity: number;
  remainingQuantity: number;
}
