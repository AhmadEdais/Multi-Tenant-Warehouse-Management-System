import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.services';
import { catchError, map, of } from 'rxjs';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.getToken()) {
    return true;
  }

  return authService.ensureCurrentUser().pipe(
    map((user) =>
      router.createUrlTree([user.roles.includes('SystemAdmin') ? '/tenants' : '/dashboard']),
    ),
    catchError(() => of(true)),
  );
};
