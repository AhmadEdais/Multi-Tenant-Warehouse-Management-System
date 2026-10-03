import { Routes } from '@angular/router';
import { authGuard } from './features/auth/auth.guard';
import { guestGuard } from './features/auth/guest-guard';
import { roleGuard } from './features/auth/role.guard';
export const routes: Routes = [
  // -----------------------
  // PUBLIC WEBSITE
  // -----------------------
  {
    path: '',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./layouts/public-layout/public-layout').then((m) => m.PublicLayout),

    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/landing/landing-page/landing-page').then((m) => m.LandingPage),
      },
    ],
  },

  // -----------------------
  // AUTH
  // -----------------------
  {
    path: '',
    canActivate: [guestGuard],

    loadComponent: () => import('./layouts/auth-layout/auth-layout').then((m) => m.AuthLayout),

    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
      },
    ],
  },

  // -----------------------
  // MAIN WMS
  // -----------------------
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layouts/app-layout/app-layout').then((m) => m.AppLayout),

    children: [
      {
        path: 'dashboard',
        canActivate: [roleGuard],
        data: { roles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'] },
        loadComponent: () =>
          import('./features/dashboard/dashboard-page/dashboard-page').then((m) => m.DashboardPage),
      },
      {
        path: 'tenants',
        canActivate: [roleGuard],
        data: { roles: ['SystemAdmin'] },
        loadComponent: () =>
          import('./features/tenants/tenants-page/tenants-page').then((m) => m.TenantsPage),
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { roles: ['TenantAdmin'] },
        loadComponent: () =>
          import('./features/users/users-page/users-page').then((m) => m.UsersPage),
      },
      {
        path: 'locations',
        canActivate: [roleGuard],
        data: { roles: ['TenantAdmin'] },
        loadComponent: () =>
          import('./features/locations/locations-page/locations-page').then((m) => m.LocationsPage),
      },
      {
        path: 'warehouses',
        canActivate: [roleGuard],
        data: { roles: ['TenantAdmin'] },
        loadComponent: () =>
          import('./features/warehouses/warehouses-page/warehouses-page').then(
            (m) => m.WarehousesPage,
          ),
      },
      {
        path: 'categories',
        canActivate: [roleGuard],
        data: { roles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'] },
        loadComponent: () =>
          import('./features/categories/categories-page/categories-page').then((m) => m.CategoriesPage),
      },
      {
        path: 'products',
        canActivate: [roleGuard],
        data: { roles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'] },
        loadComponent: () =>
          import('./features/products/products-page/products-page').then((m) => m.ProductsPage),
      },
      {
        path: 'no-access',
        loadComponent: () => import('./pages/no-access/no-access').then((m) => m.NoAccess),
      },
    ],
  },

  // -----------------------
  // 404
  // -----------------------
  {
    path: '**',
    pathMatch: 'full',

    loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound),
  },
];
