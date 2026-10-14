import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import { CreateSalesOrderPickupRequest, CreateSalesOrderRequest, ListSalesOrdersParams, SalesOrderAvailabilityRequest, SalesOrderAvailabilityResult, SalesOrderDetails, SalesOrderListItem, SalesOrderPickupResult, UpdateSalesOrderRequest } from './models/sales-order';
import { SalesOrderPickup } from './models/sales-order-pickup';

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

  checkAvailability(request: SalesOrderAvailabilityRequest): Observable<SalesOrderAvailabilityResult> {
    return this.http.post<SalesOrderAvailabilityResult>(`${this.url}/availability-check`, request);
  }

  approveSalesOrder(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/approve`, null);
  }

  cancelSalesOrder(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/cancel`, null);
  }

  createSalesOrderPickup(id: number, request: CreateSalesOrderPickupRequest): Observable<SalesOrderPickupResult> {
    return this.http.post<SalesOrderPickupResult>(`${this.url}/${id}/pickups`, request);
  }

  getSalesOrderPickups(id: number): Observable<SalesOrderPickup[]> {
    return this.http.get<SalesOrderPickup[]>(`${this.url}/${id}/pickups`);
  }
}
