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
  saveToken(token: string, rememberMe: boolean) {
    localStorage.removeItem('authToken');
    sessionStorage.removeItem('authToken');

    if (rememberMe) {
      localStorage.setItem('authToken', token);
    } else {
      sessionStorage.setItem('authToken', token);
    }
  }
  getToken(): string | null {
    return localStorage.getItem('authToken') ?? sessionStorage.getItem('authToken');
  }
  logout() {
    this.clearToken();
  }

  clearToken() {
    localStorage.removeItem('authToken');
    sessionStorage.removeItem('authToken');
  }
}
