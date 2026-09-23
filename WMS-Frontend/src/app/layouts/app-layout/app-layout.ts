import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from '../../features/auth/auth.services';

interface NavigationItem {
  label: string;
  route: string;
  allowedRoles: readonly string[];
  icon: 'dashboard';
}

const NAVIGATION_ITEMS: readonly NavigationItem[] = [
  {
    label: 'Dashboard',
    route: '/dashboard',
    allowedRoles: ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'],
    icon: 'dashboard',
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

  readonly currentUser = this.authService.currentUser;
  readonly sidebarOpen = signal(false);

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
    return (
      this.navigationItems().find((item) => item.route === path)?.label ?? this.workspaceLabel()
    );
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
    this.authService.logout();
    this.closeSidebar();
    void this.router.navigateByUrl('/login');
  }
}
