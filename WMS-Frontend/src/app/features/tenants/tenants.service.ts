import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../../shared/models/paged-result';
import {
  ListTenantsParams,
  ProvisionTenantRequest,
  ProvisionTenantResponse,
  Tenant,
} from './models/tenant';

@Injectable({ providedIn: 'root' })
export class TenantsService {
  private readonly http = inject(HttpClient);
  private readonly url = 'https://localhost:7105/api/v1/Tenants';

  list(params: ListTenantsParams): Observable<PagedResult<Tenant>> {
    let query = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);

    if (params.search) {
      query = query.set('search', params.search);
    }
    if (params.isActive !== null) {
      query = query.set('isActive', params.isActive);
    }

    return this.http.get<PagedResult<Tenant>>(this.url, { params: query });
  }

  provision(request: ProvisionTenantRequest): Observable<ProvisionTenantResponse> {
    return this.http.post<ProvisionTenantResponse>(`${this.url}/provision`, request);
  }

  suspend(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/suspend`, null);
  }

  reactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/reactivate`, null);
  }
}
