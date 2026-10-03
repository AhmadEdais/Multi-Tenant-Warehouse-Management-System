export interface Supplier {
  id: number;
  code: string;
  name: string;
  contactEmail: string | null;
  phoneNumber: string;
  address: string | null;
  isActive: boolean;
}

export interface ListSuppliersParams {
  searchTerm: string;
  isActive: boolean | null;
  pageNumber: number;
  pageSize: number;
}

export interface UpdateSupplierRequest {
  name: string;
  contactEmail: string | null;
  phoneNumber: string;
  address: string | null;
}

export interface CreateSupplierRequest extends UpdateSupplierRequest {
  code: string;
}
