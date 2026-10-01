import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CategoryDetails,
  CategoryNode,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from './models/category';

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/Categories`;

  getTree(): Observable<CategoryNode[]> {
    return this.http.get<CategoryNode[]>(`${this.apiUrl}/tree`);
  }

  getById(id: number): Observable<CategoryDetails> {
    return this.http.get<CategoryDetails>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateCategoryRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.apiUrl, request);
  }

  update(id: number, request: UpdateCategoryRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/Update/${id}`, request);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Deactivate/${id}`, {});
  }

  reactivate(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Reactivate/${id}`, {});
  }
}
