import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService, landingRouteFor } from './auth.services';
import { catchError, map, of } from 'rxjs';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.restoreSession().pipe(
    map((user) => router.createUrlTree([landingRouteFor(user)])),
    catchError(() => of(true)),
  );
};
