import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService, landingRouteFor } from './auth.services';

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

  const user = authService.currentUser();
  return router.createUrlTree([user ? landingRouteFor(user) : '/login']);
};
