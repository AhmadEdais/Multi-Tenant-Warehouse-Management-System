import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import {
  catchError,
  defer,
  finalize,
  Observable,
  of,
  shareReplay,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
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
  constructor(override readonly cause: unknown) {
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
  private accessToken: string | null = null;
  private accessTokenExpiresAt = 0;
  private authGeneration = 0;
  private sessionKnownInvalid = false;
  private refreshInFlight$: Observable<LoginResponse> | null = null;
  private restoreInFlight$: Observable<CurrentUser> | null = null;
  private logoutInFlight$: Observable<void> | null = null;
  private logoutPending = false;

  constructor() {
    this.removeLegacyToken();
  }

  login(request: LoginRequest): Observable<CurrentUser> {
    return defer(() => {
      this.clearSession();
      const generation = this.authGeneration;
      return this.http
        .post<LoginResponse>(`${this.apiBaseUrl}/Authorization/login`, request, {
          withCredentials: true,
        })
        .pipe(
          tap((response) => {
            this.acceptAccessToken(response, generation);
            this.currentUserState.set(null);
          }),
          switchMap(() =>
            this.loadCurrentUser().pipe(
              catchError((error) => throwError(() => new SessionInitializationError(error))),
            ),
          ),
        );
    });
  }

  refreshAccessToken(): Observable<LoginResponse> {
    if (this.refreshInFlight$) return this.refreshInFlight$;

    const generation = this.authGeneration;
    this.refreshInFlight$ = this.http
      .post<LoginResponse>(`${this.apiBaseUrl}/Authorization/refresh`, null, {
        withCredentials: true,
      })
      .pipe(
        tap((response) => this.acceptAccessToken(response, generation)),
        catchError((error: unknown) => {
          if (
            error instanceof HttpErrorResponse &&
            error.status === 401 &&
            generation === this.authGeneration
          ) {
            this.clearSession();
          }
          return throwError(() => error);
        }),
        finalize(() => (this.refreshInFlight$ = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    return this.refreshInFlight$;
  }

  restoreSession(): Observable<CurrentUser> {
    const user = this.currentUser();
    if (user && this.getToken()) return of(user);
    if (this.sessionKnownInvalid) {
      return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }));
    }
    if (this.restoreInFlight$) return this.restoreInFlight$;

    const accessReady$: Observable<LoginResponse | null> = this.getToken()
      ? of(null)
      : this.refreshAccessToken();
    this.restoreInFlight$ = accessReady$.pipe(
      switchMap(() => this.loadCurrentUser()),
      finalize(() => (this.restoreInFlight$ = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.restoreInFlight$;
  }

  loadCurrentUser(): Observable<CurrentUser> {
    const url = `${this.apiBaseUrl}/users/me`;
    const generation = this.authGeneration;
    return this.http.get<CurrentUser>(url).pipe(
      tap((user) => {
        if (generation !== this.authGeneration) throw new Error('Authentication state changed.');
        this.currentUserState.set(user);
      }),
      catchError((error: unknown) => {
        if (
          error instanceof HttpErrorResponse &&
          error.status === 401 &&
          generation === this.authGeneration
        ) {
          this.clearSession();
        }
        return throwError(() => error);
      }),
    );
  }

  ensureCurrentUser(): Observable<CurrentUser> {
    if (!this.getToken()) {
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

  getToken(): string | null {
    return this.accessToken && Date.now() < this.accessTokenExpiresAt ? this.accessToken : null;
  }

  hasSessionToRecover(): boolean {
    return !this.logoutPending && (this.accessToken !== null || this.currentUser() !== null);
  }

  getSessionGeneration(): number {
    return this.authGeneration;
  }

  isSessionKnownInvalid(): boolean {
    return this.sessionKnownInvalid;
  }

  logout(): Observable<void> {
    if (this.logoutInFlight$) return this.logoutInFlight$;

    this.logoutInFlight$ = defer(() => {
      this.logoutPending = true;
      return this.http.post<void>(`${this.apiBaseUrl}/Authorization/logout`, null, {
        withCredentials: true,
      });
    }).pipe(
      tap(() => this.clearSession()),
      finalize(() => {
        this.logoutPending = false;
        this.logoutInFlight$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.logoutInFlight$;
  }

  clearSession(): void {
    this.authGeneration++;
    this.sessionKnownInvalid = true;
    this.accessToken = null;
    this.accessTokenExpiresAt = 0;
    this.currentUserState.set(null);
    this.removeLegacyToken();
  }

  private acceptAccessToken(response: LoginResponse, generation: number): void {
    if (generation !== this.authGeneration) throw new Error('Authentication state changed.');

    const expiresAt = Date.now() + response?.expiresIn * 1000;
    if (
      typeof response?.accessToken !== 'string' ||
      !response.accessToken.trim() ||
      !Number.isSafeInteger(response.expiresIn) ||
      response.expiresIn <= 0 ||
      !Number.isSafeInteger(expiresAt)
    ) {
      this.clearSession();
      throw new Error('Invalid authentication response.');
    }

    this.accessToken = response.accessToken;
    this.accessTokenExpiresAt = expiresAt;
    this.sessionKnownInvalid = false;
  }

  private removeLegacyToken(): void {
    try {
      if (typeof localStorage !== 'undefined') localStorage.removeItem('authToken');
      if (typeof sessionStorage !== 'undefined') sessionStorage.removeItem('authToken');
    } catch {
      // Browser storage can be unavailable; it is never used for authentication now.
    }
  }
}
