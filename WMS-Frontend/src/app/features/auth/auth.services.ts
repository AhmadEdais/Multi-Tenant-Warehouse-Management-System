import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LoginRequest } from './models/login-request';
import { LoginResponse } from './models/login-response';
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);

  login(request: LoginRequest): Observable<LoginResponse> {
    const url = 'https://localhost:7105/api/v1/Authorization/login';
    return this.http.post<LoginResponse>(url, request);
  }
}
