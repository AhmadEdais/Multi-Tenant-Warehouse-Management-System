export interface Warehouse {
  id: number;
  code: string;
  name: string;
  address: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ListWarehousesParams {
  pageNumber: number;
  pageSize: number;
  search: string;
  isActive: boolean | null;
}

export interface CreateWarehouseRequest {
  code: string;
  name: string;
  address: string | null;
}

export interface UpdateWarehouseRequest {
  name: string;
  address: string | null;
}

export interface CreateWarehouseResponse {
  id: number;
}
