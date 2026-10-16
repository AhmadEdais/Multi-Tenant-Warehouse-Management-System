import { PageHeaderComponent } from '../../../shared/components/page-header/page-header';
import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of, startWith, Subject, switchMap } from 'rxjs';
import { AuthService } from '../../auth/auth.services';
import {
  ListPurchaseOrdersParams,
  PurchaseOrderListItem,
} from '../../purchase-orders/models/purchase-order';
import { PurchaseOrderStatusBadge } from '../../purchase-orders/purchase-order-status-badge/purchase-order-status-badge';
import { PurchaseOrdersService } from '../../purchase-orders/purchase-orders.service';
import { MANAGE_INBOUND_ROLES } from '../../purchase-orders/permissions';
import { PUTAWAY_ROLES } from '../../putaway/permissions';
import { ListSalesOrdersParams, SalesOrderListItem } from '../../sales-orders/models/sales-order';
import { MANAGE_OUTBOUND_ROLES } from '../../sales-orders/permissions';
import { SalesOrderStatusBadge } from '../../sales-orders/sales-order-status-badge/sales-order-status-badge';
import { SalesOrdersService } from '../../sales-orders/sales-orders.service';

interface WorkspaceLink {
  title: string;
  description: string;
  route: string;
  icon: 'inventory' | 'purchase-orders' | 'sales-orders' | 'putaway' | 'products' | 'warehouses';
  allowedRoles: readonly string[];
}

interface QuickAction {
  label: string;
  route: string;
  allowedRoles: readonly string[];
  primary?: boolean;
}

const VIEW_ORDER_ROLES = [
  'TenantAdmin',
  'WarehouseManager',
  'WarehouseOperator',
  'Analyst',
] as const;
const VIEW_INVENTORY_ROLES = ['TenantAdmin', 'WarehouseManager', 'Analyst'] as const;
const ROLE_PRIORITY = ['TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'] as const;
const ROLE_LABELS: Record<string, string> = {
  TenantAdmin: 'Tenant Admin',
  WarehouseManager: 'Warehouse Manager',
  WarehouseOperator: 'Warehouse Operator',
  Analyst: 'Analyst',
};

const WORKSPACE_LINKS: readonly WorkspaceLink[] = [
  {
    title: 'Inventory',
    description: 'View stock by warehouse and location.',
    route: '/inventory',
    icon: 'inventory',
    allowedRoles: VIEW_INVENTORY_ROLES,
  },
  {
    title: 'Purchase Orders',
    description: 'Track supplier orders and receiving.',
    route: '/purchase-orders',
    icon: 'purchase-orders',
    allowedRoles: VIEW_ORDER_ROLES,
  },
  {
    title: 'Sales Orders',
    description: 'Track customer orders and pickups.',
    route: '/sales-orders',
    icon: 'sales-orders',
    allowedRoles: VIEW_ORDER_ROLES,
  },
  {
    title: 'Putaway',
    description: 'Move dock stock into warehouse Bins.',
    route: '/putaway',
    icon: 'putaway',
    allowedRoles: PUTAWAY_ROLES,
  },
  {
    title: 'Products',
    description: 'Browse SKUs and product details.',
    route: '/products',
    icon: 'products',
    allowedRoles: VIEW_ORDER_ROLES,
  },
  {
    title: 'Warehouses',
    description: 'Manage warehouses and locations.',
    route: '/warehouses',
    icon: 'warehouses',
    allowedRoles: ['TenantAdmin'],
  },
];

const QUICK_ACTIONS: readonly QuickAction[] = [
  {
    label: 'Create Purchase Order',
    route: '/purchase-orders/new',
    allowedRoles: MANAGE_INBOUND_ROLES,
    primary: true,
  },
  {
    label: 'Create Sales Order',
    route: '/sales-orders/new',
    allowedRoles: MANAGE_OUTBOUND_ROLES,
    primary: true,
  },
  { label: 'View Inventory', route: '/inventory', allowedRoles: VIEW_INVENTORY_ROLES },
  { label: 'Open Putaway Queue', route: '/putaway', allowedRoles: PUTAWAY_ROLES },
  {
    label: 'View Purchase Orders',
    route: '/purchase-orders',
    allowedRoles: ['WarehouseOperator', 'Analyst'],
  },
  {
    label: 'View Sales Orders',
    route: '/sales-orders',
    allowedRoles: ['WarehouseOperator', 'Analyst'],
  },
];

const RECENT_PURCHASE_ORDERS: ListPurchaseOrdersParams = {
  search: '',
  status: null,
  supplierId: null,
  warehouseId: null,
  pageNumber: 1,
  pageSize: 5,
};
const RECENT_SALES_ORDERS: ListSalesOrdersParams = {
  search: '',
  status: null,
  customerId: null,
  warehouseId: null,
  pageNumber: 1,
  pageSize: 5,
};

@Component({
  imports: [DatePipe, RouterLink, PurchaseOrderStatusBadge, SalesOrderStatusBadge, PageHeaderComponent],
  selector: 'app-dashboard-page',
  styleUrl: './dashboard-page.css',
  templateUrl: './dashboard-page.html',
})
export class DashboardPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly purchaseOrdersService = inject(PurchaseOrdersService);
  private readonly salesOrdersService = inject(SalesOrdersService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reloadPurchaseOrders$ = new Subject<void>();
  private readonly reloadSalesOrders$ = new Subject<void>();

  readonly currentUser = this.auth.currentUser;
  readonly today = new Date();
  readonly canUseDashboard = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    return !roles.includes('SystemAdmin') && VIEW_ORDER_ROLES.some((role) => roles.includes(role));
  });
  readonly firstName = computed(
    () => this.currentUser()?.fullName.trim().split(/\s+/)[0] || 'there',
  );
  readonly primaryRole = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    const role = ROLE_PRIORITY.find((candidate) => roles.includes(candidate));
    return role ? ROLE_LABELS[role] : 'Warehouse user';
  });
  readonly workspaceLinks = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    if (!this.canUseDashboard()) return [];
    return WORKSPACE_LINKS.filter((link) => link.allowedRoles.some((role) => roles.includes(role)));
  });
  readonly quickActions = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    if (!this.canUseDashboard()) return [];
    return QUICK_ACTIONS.filter((action) =>
      action.allowedRoles.some((role) => roles.includes(role)),
    );
  });

  readonly purchaseOrders = signal<PurchaseOrderListItem[]>([]);
  readonly purchaseOrdersLoading = signal(true);
  readonly purchaseOrdersError = signal<string | null>(null);
  readonly salesOrders = signal<SalesOrderListItem[]>([]);
  readonly salesOrdersLoading = signal(true);
  readonly salesOrdersError = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.canUseDashboard()) return;

    this.reloadPurchaseOrders$
      .pipe(
        startWith(undefined),
        switchMap(() => {
          this.purchaseOrdersLoading.set(true);
          this.purchaseOrdersError.set(null);
          return this.purchaseOrdersService.listPurchaseOrders(RECENT_PURCHASE_ORDERS).pipe(
            catchError(() => {
              this.purchaseOrdersError.set('Could not load recent Purchase Orders.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.purchaseOrders.set(result?.data ?? []);
        this.purchaseOrdersLoading.set(false);
      });

    this.reloadSalesOrders$
      .pipe(
        startWith(undefined),
        switchMap(() => {
          this.salesOrdersLoading.set(true);
          this.salesOrdersError.set(null);
          return this.salesOrdersService.listSalesOrders(RECENT_SALES_ORDERS).pipe(
            catchError(() => {
              this.salesOrdersError.set('Could not load recent Sales Orders.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.salesOrders.set(result?.data ?? []);
        this.salesOrdersLoading.set(false);
      });
  }

  retryPurchaseOrders(): void {
    this.reloadPurchaseOrders$.next();
  }

  retrySalesOrders(): void {
    this.reloadSalesOrders$.next();
  }
}
