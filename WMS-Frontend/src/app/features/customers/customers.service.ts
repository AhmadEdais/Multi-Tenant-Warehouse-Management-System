import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import {
  CreateCustomerRequest,
  ListCustomersParams,
  Customer,
  UpdateCustomerRequest,
} from './models/customer';

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/Customers`;

  list(params: ListCustomersParams): Observable<PagedResult<Customer>> {
    let query = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);
    if (params.searchTerm) query = query.set('searchTerm', params.searchTerm);
    if (params.isActive !== null) query = query.set('isActive', params.isActive);
    return this.http.get<PagedResult<Customer>>(this.url, { params: query });
  }

  getById(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.url}/${id}`);
  }

  create(request: CreateCustomerRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.url, request);
  }

  update(id: number, request: UpdateCustomerRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, request);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/deactivate`, {});
  }

  reactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/reactivate`, {});
  }
}
