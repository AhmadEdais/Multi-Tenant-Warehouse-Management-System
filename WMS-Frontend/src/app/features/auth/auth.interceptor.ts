import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, of, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.services';

let redirectingToLogin = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const apiUrl = new URL(environment.apiBaseUrl, document.baseURI);
  const requestUrl = new URL(req.url, document.baseURI);
  const apiPath = apiUrl.pathname.replace(/\/$/, '');

  if (requestUrl.origin !== apiUrl.origin || !requestUrl.pathname.startsWith(`${apiPath}/`)) {
    return next(req);
  }

  const endpoint = requestUrl.pathname.slice(apiPath.length);
  if (req.method === 'POST' && /^\/Authorization\/(login|refresh|logout)\/?$/i.test(endpoint)) {
    return next(req);
  }

  const sessionGeneration = authService.getSessionGeneration();

  const withBearer = (token: string): HttpRequest<unknown> =>
    req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

  const redirectAfterConfirmedFailure = (): void => {
    if (redirectingToLogin || router.url === '/login' || router.url === '/') return;
    redirectingToLogin = true;
    void router.navigateByUrl('/login').finally(() => (redirectingToLogin = false));
  };

  const send = (token: string) =>
    next(withBearer(token)).pipe(
      catchError((error: unknown) => {
        if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
          return throwError(() => error);
        }

        if (authService.getSessionGeneration() !== sessionGeneration) {
          return throwError(() => error);
        }

        // Another request may have installed a newer access token already.
        const newerToken = authService.getToken();
        if ((!newerToken || newerToken === token) && !authService.hasSessionToRecover()) {
          return throwError(() => error);
        }
        const renewedToken$ =
          newerToken && newerToken !== token
            ? of(newerToken)
            : authService.refreshAccessToken().pipe(map(() => authService.getToken()));

        return renewedToken$.pipe(
          switchMap((renewedToken) => {
            if (!renewedToken || authService.getSessionGeneration() !== sessionGeneration) {
              return throwError(() => error);
            }
            // The retried response does not enter this recovery branch again.
            return next(withBearer(renewedToken)).pipe(
              catchError((retryError: unknown) => {
                if (
                  retryError instanceof HttpErrorResponse &&
                  retryError.status === 401 &&
                  authService.getSessionGeneration() === sessionGeneration &&
                  authService.getToken() === renewedToken
                ) {
                  authService.clearSession();
                  redirectAfterConfirmedFailure();
                }
                return throwError(() => retryError);
              }),
            );
          }),
          catchError((recoveryError: unknown) => {
            if (
              recoveryError instanceof HttpErrorResponse &&
              recoveryError.status === 401 &&
              authService.isSessionKnownInvalid()
            ) {
              redirectAfterConfirmedFailure();
            }
            return throwError(() => recoveryError);
          }),
        );
      }),
    );

  const token = authService.getToken();
  if (token) return send(token);

  if (!authService.hasSessionToRecover()) return next(req);

  return authService.refreshAccessToken().pipe(
    switchMap(() => {
      if (authService.getSessionGeneration() !== sessionGeneration) {
        return throwError(() => new Error('Authentication state changed.'));
      }
      const renewedToken = authService.getToken();
      return renewedToken
        ? send(renewedToken)
        : throwError(() => new Error('No access token available.'));
    }),
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        authService.isSessionKnownInvalid()
      ) {
        redirectAfterConfirmedFailure();
      }
      return throwError(() => error);
    }),
  );
};
