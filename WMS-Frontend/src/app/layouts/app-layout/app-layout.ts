import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, finalize, map } from 'rxjs';
import { AuthService } from '../../features/auth/auth.services';
import { PUTAWAY_ROLES } from '../../features/putaway/permissions';

interface NavigationItem {
  label: string;
  route: string;
  allowedRoles: readonly string[];
  icon:
    | 'dashboard'
    | 'tenants'
    | 'users'
    | 'warehouses'
    | 'locations'
    | 'categories'
    | 'products'
    | 'inventory'
    | 'putaway'
    | 'purchase-orders'
    | 'sales-orders'
    | 'suppliers'
    | 'customers';
}

const NAVIGATION_ITEMS: readonly NavigationItem[] = [
  {
    label: 'Tenants',
    route: '/tenants',
    allowedRoles: ['SystemAdmin'],
    icon: 'tenants',
  },
  {
    label: 'Dashboard',
    route: '/dashboard',
    allowedRoles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'],
    icon: 'dashboard',
  },
  {
    label: 'Users',
    route: '/users',
    allowedRoles: ['TenantAdmin'],
    icon: 'users',
  },
  {
    label: 'Warehouses',
    route: '/warehouses',
    allowedRoles: ['TenantAdmin'],
    icon: 'warehouses',
  },
  {
    label: 'Locations',
    route: '/locations',
    allowedRoles: ['TenantAdmin'],
    icon: 'locations',
  },
  {
    label: 'Categories',
    route: '/categories',
    allowedRoles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'],
    icon: 'categories',
  },
  {
    label: 'Products',
    route: '/products',
    allowedRoles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'],
    icon: 'products',
  },
  {
    label: 'Inventory',
    route: '/inventory',
    allowedRoles: ['TenantAdmin', 'WarehouseManager', 'Analyst'],
    icon: 'inventory',
  },
  {
    label: 'Putaway',
    route: '/putaway',
    allowedRoles: PUTAWAY_ROLES,
    icon: 'putaway',
  },
  {
    label: 'Purchase Orders',
    route: '/purchase-orders',
    allowedRoles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'],
    icon: 'purchase-orders',
  },
  {
    label: 'Sales Orders',
    route: '/sales-orders',
    allowedRoles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'],
    icon: 'sales-orders',
  },
  {
    label: 'Suppliers',
    route: '/suppliers',
    allowedRoles: ['TenantAdmin', 'WarehouseManager'],
    icon: 'suppliers',
  },
  {
    label: 'Customers',
    route: '/customers',
    allowedRoles: ['TenantAdmin', 'WarehouseManager'],
    icon: 'customers',
  },
];

const ROLE_LABELS: Record<string, string> = {
  SystemAdmin: 'System Admin',
  TenantAdmin: 'Tenant Admin',
  WarehouseManager: 'Warehouse Manager',
  WarehouseOperator: 'Warehouse Operator',
  Analyst: 'Analyst',
};

const ROLE_PRIORITY = [
  'SystemAdmin',
  'TenantAdmin',
  'WarehouseManager',
  'WarehouseOperator',
  'Analyst',
];

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-app-layout',
  styleUrl: './app-layout.css',
  templateUrl: './app-layout.html',
})
export class AppLayout {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly currentUser = this.authService.currentUser;
  readonly sidebarOpen = signal(false);
  readonly isLoggingOut = signal(false);
  readonly logoutError = signal<string | null>(null);

  private readonly routeUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  readonly navigationItems = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    if (roles.includes('SystemAdmin')) {
      return NAVIGATION_ITEMS.filter((item) => item.allowedRoles.includes('SystemAdmin'));
    }

    return NAVIGATION_ITEMS.filter((item) =>
      item.allowedRoles.some((role) => roles.includes(role)),
    );
  });

  readonly workspaceLabel = computed(() =>
    this.currentUser()?.roles.includes('SystemAdmin') ? 'Platform workspace' : 'Tenant workspace',
  );

  readonly pageTitle = computed(() => {
    const path = this.routeUrl().split(/[?#]/, 1)[0];
    if (path === '/sales-orders/new') return 'Create Sales Order';
    if (/^\/sales-orders\/[^/]+\/edit$/.test(path)) return 'Edit Sales Order';
    return (
      this.navigationItems().find(
        (item) =>
          item.route === path ||
          (item.route === '/purchase-orders' && path.startsWith('/purchase-orders/')) ||
          (item.route === '/sales-orders' && path.startsWith('/sales-orders/')),
      )?.label ?? this.workspaceLabel()
    );
  });

  readonly breadcrumb = computed(() => {
    const path = this.routeUrl().split(/[?#]/, 1)[0];
    if (path === '/dashboard') return null;

    const platform = this.currentUser()?.roles.includes('SystemAdmin') ?? false;
    const rootLabel = platform ? 'Platform' : 'Workspace';
    const rootRoute = platform ? null : '/dashboard';
    const orderSection = path.startsWith('/purchase-orders/')
      ? { label: 'Purchase Orders', route: '/purchase-orders' }
      : path.startsWith('/sales-orders/')
        ? { label: 'Sales Orders', route: '/sales-orders' }
        : null;

    if (orderSection) {
      const currentLabel = path.endsWith('/new')
        ? 'Create'
        : path.endsWith('/edit')
          ? 'Edit'
          : 'Details';
      return { rootLabel, rootRoute, parent: orderSection, currentLabel };
    }

    return { rootLabel, rootRoute, parent: null, currentLabel: this.pageTitle() };
  });

  readonly primaryRole = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    const role = ROLE_PRIORITY.find((name) => roles.includes(name)) ?? roles[0];
    return role ? (ROLE_LABELS[role] ?? role) : 'User';
  });

  readonly initials = computed(() => {
    const name = this.currentUser()?.fullName.trim();
    if (!name) {
      return 'WM';
    }

    const parts = name.split(/\s+/);
    return (
      parts.length > 1 ? `${parts[0][0]}${parts.at(-1)?.[0]}` : name.slice(0, 2)
    ).toUpperCase();
  });

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  logout(): void {
    if (this.isLoggingOut()) return;
    this.logoutError.set(null);
    this.isLoggingOut.set(true);
    this.authService
      .logout()
      .pipe(
        finalize(() => this.isLoggingOut.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.closeSidebar();
          void this.router.navigateByUrl('/login');
        },
        error: (error: unknown) => {
          this.logoutError.set(
            error instanceof HttpErrorResponse && error.status === 403
              ? 'Log out was blocked. Please try again from this workspace.'
              : 'Could not log out. Please try again.',
          );
        },
      });
  }
}
