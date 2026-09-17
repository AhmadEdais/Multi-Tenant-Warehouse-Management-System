import { Routes } from '@angular/router';
import { authGuard } from './features/auth/auth.guard';
import { guestGuard } from './features/auth/guest-guard';
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
      {
        path: 'signup',
        loadComponent: () => import('./features/auth/signup/signup').then((m) => m.Signup),
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
        loadComponent: () =>
          import('./features/dashboard/dashboard-page/dashboard-page').then((m) => m.DashboardPage),
      },
    ],
  },

  // -----------------------
  // 404
  // -----------------------
  {
    path: '**',
    loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound),
  },
];
