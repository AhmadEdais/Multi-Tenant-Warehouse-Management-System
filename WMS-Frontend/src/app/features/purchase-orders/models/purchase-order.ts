export type PurchaseOrderStatus = 'Draft' | 'Pending' | 'Receiving' | 'Received' | 'Canceled';

export interface PurchaseOrderListItem {
  id: number;
  orderNumber: string;
  status: PurchaseOrderStatus;
  supplierId: number;
  supplierCode: string;
  supplierName: string;
  warehouseId: number;
  warehouseCode: string;
  warehouseName: string;
  expectedDeliveryDate: string | null;
  createdOnUtc: string;
  totalAmount: number;
  totalExpectedQuantity: number;
  totalReceivedQuantity: number;
}

export interface PurchaseOrderLineDetails {
  id: number;
  productId: number;
  productSku: string;
  productName: string;
  expectedQuantity: number;
  receivedQuantity: number;
  remainingQuantity: number;
  unitCost: number;
  lineTotal: number;
}

export interface PurchaseOrderDetails {
  id: number;
  orderNumber: string;
  status: PurchaseOrderStatus;
  supplierId: number;
  supplierCode: string;
  supplierName: string;
  warehouseId: number;
  warehouseCode: string;
  warehouseName: string;
  expectedDeliveryDate: string | null;
  createdOnUtc: string;
  createdBy: string;
  createdByFullName: string;
  lastModifiedOnUtc: string | null;
  lastModifiedBy: string | null;
  lastModifiedByFullName: string | null;
  rowVersion: string;
  totalAmount: number;
  lines: PurchaseOrderLineDetails[];
}

export interface ListPurchaseOrdersParams {
  search: string;
  status: PurchaseOrderStatus | null;
  supplierId: number | null;
  warehouseId: number | null;
  pageNumber: number;
  pageSize: number;
}

export interface CreatePurchaseOrderLineRequest {
  productId: number;
  expectedQuantity: number;
  unitCost: number;
}

export interface CreatePurchaseOrderRequest {
  supplierId: number;
  warehouseId: number;
  expectedDeliveryDate: string | null;
  lines: CreatePurchaseOrderLineRequest[];
}

export type UpdatePurchaseOrderRequest = CreatePurchaseOrderRequest;

export interface ReceivePurchaseOrderRequest {
  lines: {
    purchaseOrderLineId: number;
    quantity: number;
  }[];
}
