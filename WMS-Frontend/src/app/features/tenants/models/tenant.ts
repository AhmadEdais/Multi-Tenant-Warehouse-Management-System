export interface Tenant {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  createdByUserId: number | null;
}

export interface ListTenantsParams {
  pageNumber: number;
  pageSize: number;
  search: string;
  isActive: boolean | null;
}

export interface ProvisionTenantRequest {
  tenantCode: string;
  tenantName: string;
  adminFullName: string;
  adminEmail: string;
  initialPassword: string;
}

export interface ProvisionTenantResponse {
  id: number;
}
