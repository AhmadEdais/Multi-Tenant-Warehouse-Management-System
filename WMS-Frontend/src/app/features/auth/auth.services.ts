import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, Observable, of, switchMap, tap, throwError } from 'rxjs';
import { LoginRequest } from './models/login-request';
import { LoginResponse } from './models/login-response';
import { CurrentUser } from './models/current-user';
import { environment } from '../../../environments/environment';

export function landingRouteFor(user: CurrentUser): string {
  if (user.roles.includes('SystemAdmin')) return '/tenants';
  if (
    user.roles.some((role) =>
      ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'].includes(role),
    )
  ) {
    return '/dashboard';
  }
  return '/no-access';
}

export class SessionInitializationError extends Error {
  constructor() {
    super('Unable to load the current user.');
  }
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;
  private readonly currentUserState = signal<CurrentUser | null>(null);
  readonly currentUser = this.currentUserState.asReadonly();

  login(request: LoginRequest, rememberMe: boolean): Observable<CurrentUser> {
    const url = `${this.apiBaseUrl}/Authorization/login`;
    return this.http.post<LoginResponse>(url, request).pipe(
      switchMap(({ token }) => {
        this.saveToken(token, rememberMe);
        return this.loadCurrentUser().pipe(
          catchError(() => throwError(() => new SessionInitializationError())),
        );
      }),
    );
  }

  loadCurrentUser(): Observable<CurrentUser> {
    const url = `${this.apiBaseUrl}/users/me`;
    return this.http.get<CurrentUser>(url).pipe(
      tap((user) => this.currentUserState.set(user)),
      catchError((error) => {
        this.clearSession();
        return throwError(() => error);
      }),
    );
  }

  ensureCurrentUser(): Observable<CurrentUser> {
    const token = this.getToken();
    if (!token) {
      this.clearSession();
      return throwError(() => new Error('No active session.'));
    }

    const user = this.currentUser();
    return user ? of(user) : this.loadCurrentUser();
  }

  hasRole(role: string): boolean {
    return this.currentUser()?.roles.includes(role) ?? false;
  }

  hasAnyRole(roles: readonly string[]): boolean {
    return roles.some((role) => this.hasRole(role));
  }

  private saveToken(token: string, rememberMe: boolean): void {
    this.currentUserState.set(null);
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

  logout(): void {
    this.clearSession();
  }

  clearSession(): void {
    this.currentUserState.set(null);
    localStorage.removeItem('authToken');
    sessionStorage.removeItem('authToken');
  }
}
