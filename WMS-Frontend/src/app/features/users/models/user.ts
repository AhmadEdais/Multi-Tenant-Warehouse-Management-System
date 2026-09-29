export interface User {
  id: number;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
  roles: string[];
}

export interface AssignableRole {
  id: number;
  name: string;
  description: string;
}

export interface ListUsersParams {
  searchTerm: string;
  pageNumber: number;
  pageSize: number;
  isActive: boolean | null;
  role: string | null;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  initialPassword: string;
  roleIds: number[];
}

export interface CreateUserResponse {
  id: number;
}

export interface ReplaceUserRolesRequest {
  roleIds: number[];
}
