import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../../shared/models/paged-result';
import {
  AssignableRole,
  CreateUserRequest,
  CreateUserResponse,
  ListUsersParams,
  ReplaceUserRolesRequest,
  User,
} from './models/user';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly url = 'https://localhost:7105/api/v1/Users';

  list(params: ListUsersParams): Observable<PagedResult<User>> {
    let query = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);
    if (params.searchTerm) query = query.set('searchTerm', params.searchTerm);
    if (params.isActive !== null) query = query.set('isActive', params.isActive);
    if (params.role !== null) query = query.set('role', params.role);
    return this.http.get<PagedResult<User>>(this.url, { params: query });
  }

  assignableRoles(): Observable<AssignableRole[]> {
    return this.http.get<AssignableRole[]>('https://localhost:7105/api/v1/Roles/assignable');
  }

  create(request: CreateUserRequest): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>(this.url, request);
  }

  replaceRoles(id: number, request: ReplaceUserRolesRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/roles`, request);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/deactivate`, null);
  }

  reactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/reactivate`, null);
  }
}
