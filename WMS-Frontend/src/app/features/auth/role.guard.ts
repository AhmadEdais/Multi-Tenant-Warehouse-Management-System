import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.services';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const allowedRoles = route.data['roles'] as readonly string[];

  if (authService.hasRole('SystemAdmin') && !allowedRoles.includes('SystemAdmin')) {
    return router.createUrlTree(['/tenants']);
  }

  if (authService.hasAnyRole(allowedRoles)) {
    return true;
  }

  return router.createUrlTree([authService.hasRole('SystemAdmin') ? '/tenants' : '/dashboard']);
};
