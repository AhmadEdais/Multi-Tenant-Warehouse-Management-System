import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import {
  CreateSupplierRequest,
  ListSuppliersParams,
  Supplier,
  UpdateSupplierRequest,
} from './models/supplier';

@Injectable({ providedIn: 'root' })
export class SuppliersService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/Suppliers`;

  list(params: ListSuppliersParams): Observable<PagedResult<Supplier>> {
    let query = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);
    if (params.searchTerm) query = query.set('searchTerm', params.searchTerm);
    if (params.isActive !== null) query = query.set('isActive', params.isActive);
    return this.http.get<PagedResult<Supplier>>(this.url, { params: query });
  }

  getById(id: number): Observable<Supplier> {
    return this.http.get<Supplier>(`${this.url}/${id}`);
  }

  create(request: CreateSupplierRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.url, request);
  }

  update(id: number, request: UpdateSupplierRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, request);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/deactivate`, {});
  }

  reactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/reactivate`, {});
  }
}
