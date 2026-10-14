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
