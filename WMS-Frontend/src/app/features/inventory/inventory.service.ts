import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import {
  InventorySummaryParams,
  StockByLocation,
  StockByProduct,
  TenantStockSummary,
} from './models/inventory';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/Inventory`;

  getSummary(params: InventorySummaryParams): Observable<PagedResult<TenantStockSummary>> {
    let query = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);
    if (params.searchTerm) query = query.set('searchTerm', params.searchTerm);
    return this.http.get<PagedResult<TenantStockSummary>>(`${this.url}/summary`, { params: query });
  }

  getByProduct(
    productId: number,
    pageNumber: number,
    pageSize: number,
  ): Observable<PagedResult<StockByProduct>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<StockByProduct>>(`${this.url}/product/${productId}`, {
      params,
    });
  }

  getByLocation(
    locationId: number,
    pageNumber: number,
    pageSize: number,
  ): Observable<PagedResult<StockByLocation>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<StockByLocation>>(`${this.url}/Location/${locationId}`, {
      params,
    });
  }
}
