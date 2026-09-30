import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LocationNode } from './models/location';

@Injectable({
  providedIn: 'root',
})
export class LocationsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/Locations`;

  getTree(warehouseId: number): Observable<LocationNode[]> {
    return this.http.get<LocationNode[]>(`${this.apiUrl}/warehouse/${warehouseId}/tree`);
  }
}
