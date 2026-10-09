import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result';
import { PutawayQueueItem, PutawayQueueParams, PutawayRequest } from './models/putaway';

@Injectable({ providedIn: 'root' })
export class PutawayService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/putaway`;

  getQueue(query: PutawayQueueParams): Observable<PagedResult<PutawayQueueItem>> {
    let params = new HttpParams()
      .set('warehouseId', query.warehouseId)
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
    return this.http.get<PagedResult<PutawayQueueItem>>(this.url, { params });
  }

  putAway(request: PutawayRequest): Observable<void> {
    return this.http.post<void>(this.url, request);
  }
}
