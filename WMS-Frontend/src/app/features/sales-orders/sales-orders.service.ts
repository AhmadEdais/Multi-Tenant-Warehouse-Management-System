import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import { CreateSalesOrderRequest, ListSalesOrdersParams, SalesOrderDetails, SalesOrderListItem, UpdateSalesOrderRequest } from './models/sales-order';

@Injectable({ providedIn: 'root' })
export class SalesOrdersService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/outbound/sales-orders`;

  listSalesOrders(params: ListSalesOrdersParams): Observable<PagedResult<SalesOrderListItem>> {
    let query = new HttpParams()
      .set('PageNumber', params.pageNumber)
      .set('PageSize', params.pageSize);
    if (params.search) query = query.set('Search', params.search);
    if (params.status) query = query.set('Status', params.status);
    if (params.customerId !== null) query = query.set('CustomerId', params.customerId);
    if (params.warehouseId !== null) query = query.set('WarehouseId', params.warehouseId);
    return this.http.get<PagedResult<SalesOrderListItem>>(this.url, { params: query });
  }

  createSalesOrder(request: CreateSalesOrderRequest): Observable<number> {
    return this.http.post<number>(this.url, request);
  }

  getSalesOrderById(id: number): Observable<SalesOrderDetails> {
    return this.http.get<SalesOrderDetails>(`${this.url}/${id}`);
  }

  updateSalesOrder(id: number, request: UpdateSalesOrderRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, request);
  }
}
