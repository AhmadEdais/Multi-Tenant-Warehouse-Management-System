import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import {
  CreateProductRequest,
  ListProductsParams,
  ProductDetails,
  ProductListItem,
  UpdateProductRequest,
} from './models/product';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/Products`;

  list(params: ListProductsParams): Observable<PagedResult<ProductListItem>> {
    let query = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);
    if (params.searchTerm) query = query.set('searchTerm', params.searchTerm);
    if (params.categoryId !== null) query = query.set('categoryId', params.categoryId);
    if (params.isActive !== null) query = query.set('isActive', params.isActive);
    return this.http.get<PagedResult<ProductListItem>>(this.url, { params: query });
  }

  getById(id: number): Observable<ProductDetails> {
    return this.http.get<ProductDetails>(`${this.url}/${id}`);
  }

  create(request: CreateProductRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.url, request);
  }

  update(id: number, request: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/Update/${id}`, request);
  }

  assignCategories(id: number, categoryIds: number[]): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/categories`, categoryIds);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/Deactivate/${id}`, {});
  }

  reactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/Reactivate/${id}`, {});
  }
}
