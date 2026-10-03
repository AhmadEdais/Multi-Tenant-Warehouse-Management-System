export interface Customer {
  id: number;
  code: string;
  name: string;
  contactEmail: string | null;
  phoneNumber: string;
  address: string | null;
  creditLimit: number | null;
  isActive: boolean;
}

export interface ListCustomersParams {
  searchTerm: string;
  isActive: boolean | null;
  pageNumber: number;
  pageSize: number;
}

export interface UpdateCustomerRequest {
  name: string;
  contactEmail: string | null;
  phoneNumber: string;
  address: string | null;
  creditLimit: number | null;
}

export interface CreateCustomerRequest extends UpdateCustomerRequest {
  code: string;
}
