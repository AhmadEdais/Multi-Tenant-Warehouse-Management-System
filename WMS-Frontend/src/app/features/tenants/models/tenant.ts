export interface Tenant {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  createdByUserId: number | null;
}

export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface ListTenantsParams {
  pageNumber: number;
  pageSize: number;
  search: string;
  isActive: boolean | null;
}
