import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import {
  CreatePurchaseOrderRequest,
  ListPurchaseOrdersParams,
  PurchaseOrderDetails,
  PurchaseOrderListItem,
  ReceivePurchaseOrderRequest,
  UpdatePurchaseOrderRequest,
} from './models/purchase-order';

@Injectable({ providedIn: 'root' })
export class PurchaseOrdersService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/inbound/purchase-orders`;

  listPurchaseOrders(
    params: ListPurchaseOrdersParams,
  ): Observable<PagedResult<PurchaseOrderListItem>> {
    let query = new HttpParams()
      .set('PageNumber', params.pageNumber)
      .set('PageSize', params.pageSize);
    if (params.search) query = query.set('Search', params.search);
    if (params.status) query = query.set('Status', params.status);
    if (params.supplierId !== null) query = query.set('SupplierId', params.supplierId);
    if (params.warehouseId !== null) query = query.set('WarehouseId', params.warehouseId);
    return this.http.get<PagedResult<PurchaseOrderListItem>>(this.url, { params: query });
  }

  getPurchaseOrderById(id: number): Observable<PurchaseOrderDetails> {
    return this.http.get<PurchaseOrderDetails>(`${this.url}/${id}`);
  }

  createPurchaseOrder(request: CreatePurchaseOrderRequest): Observable<number> {
    return this.http.post<number>(this.url, request);
  }

  updatePurchaseOrder(id: number, request: UpdatePurchaseOrderRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, request);
  }

  approvePurchaseOrder(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/approve`, null);
  }

  receivePurchaseOrder(id: number, request: ReceivePurchaseOrderRequest): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/receive`, request);
  }

  cancelPurchaseOrder(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/cancel`, null);
  }
}
