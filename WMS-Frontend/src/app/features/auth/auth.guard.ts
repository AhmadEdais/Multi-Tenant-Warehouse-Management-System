import { inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.services';
import { catchError, map, of } from 'rxjs';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.currentUser() && authService.getToken()) {
    return true;
  }

  return authService.restoreSession().pipe(
    map(() => true),
    catchError((error: unknown) =>
      of(
        (error instanceof HttpErrorResponse && error.status === 401) ||
        authService.isSessionKnownInvalid()
          ? router.createUrlTree(['/login'])
          : false,
      ),
    ),
  );
};
