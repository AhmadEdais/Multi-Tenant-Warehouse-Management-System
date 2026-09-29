import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import {
  CreateWarehouseRequest,
  CreateWarehouseResponse,
  ListWarehousesParams,
  UpdateWarehouseRequest,
  Warehouse,
} from './models/warehouse';

@Injectable({ providedIn: 'root' })
export class WarehousesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/Warehouses`;

  list(params: ListWarehousesParams): Observable<PagedResult<Warehouse>> {
    let query = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);
    if (params.search) query = query.set('search', params.search);
    if (params.isActive !== null) query = query.set('isActive', params.isActive);
    return this.http.get<PagedResult<Warehouse>>(this.url, { params: query });
  }

  create(request: CreateWarehouseRequest): Observable<CreateWarehouseResponse> {
    return this.http.post<CreateWarehouseResponse>(this.url, request);
  }

  update(id: number, request: UpdateWarehouseRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, request);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/deactivate`, null);
  }
}
