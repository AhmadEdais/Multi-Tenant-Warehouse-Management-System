import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateLocationRequest, LocationDetails, LocationNode, UpdateLocationRequest } from './models/location';

@Injectable({
  providedIn: 'root',
})
export class LocationsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/Locations`;

  getTree(warehouseId: number): Observable<LocationNode[]> {
    return this.http.get<LocationNode[]>(`${this.apiUrl}/warehouse/${warehouseId}/tree`);
  }

  getById(id: number): Observable<LocationDetails> {
    return this.http.get<LocationDetails>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateLocationRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.apiUrl, request);
  }

  update(id: number, request: UpdateLocationRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/Update/${id}`, request);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Deactivate/${id}`, {});
  }

  reactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Reactivate/${id}`, {});
  }
}
