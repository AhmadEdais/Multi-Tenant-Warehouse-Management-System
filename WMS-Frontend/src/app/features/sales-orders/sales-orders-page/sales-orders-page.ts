import { PageHeaderComponent } from '../../../shared/components/page-header/page-header';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  EMPTY,
  expand,
  filter,
  map,
  merge,
  of,
  reduce,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { AuthService } from '../../auth/auth.services';
import { Customer } from '../../customers/models/customer';
import { CustomersService } from '../../customers/customers.service';
import { Warehouse } from '../../warehouses/models/warehouse';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { SalesOrderListItem, SalesOrderStatus } from '../models/sales-order';
import { MANAGE_OUTBOUND_ROLES } from '../permissions';
import { SalesOrderStatusBadge } from '../sales-order-status-badge/sales-order-status-badge';
import { SalesOrdersService } from '../sales-orders.service';

@Component({
  selector: 'app-sales-orders-page',
  imports: [DatePipe, DecimalPipe, RouterLink, SalesOrderStatusBadge, PageHeaderComponent],
  templateUrl: './sales-orders-page.html',
})
export class SalesOrdersPage implements OnInit {
  private readonly ordersService = inject(SalesOrdersService);
  private readonly customersService = inject(CustomersService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  readonly canCreate = computed(
    () => !this.auth.hasRole('SystemAdmin') && this.auth.hasAnyRole(MANAGE_OUTBOUND_ROLES),
  );
  readonly canListWarehouses = this.auth.hasAnyRole([
    'TenantAdmin',
    'WarehouseManager',
    'WarehouseOperator',
  ]);
  readonly pageSize = 20;
  readonly orders = signal<SalesOrderListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly search = signal('');
  readonly appliedSearch = signal('');
  readonly status = signal<SalesOrderStatus | null>(null);
  readonly customerId = signal<number | null>(null);
  readonly warehouseId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly customers = signal<Customer[]>([]);
  readonly customersLoading = signal(false);
  readonly customersError = signal(false);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly warehousesLoading = signal(false);
  readonly warehousesError = signal(false);

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly firstItem = computed(() =>
    this.totalCount() ? (this.page() - 1) * this.pageSize + 1 : 0,
  );
  readonly lastItem = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));
  readonly hasFilters = computed(
    () =>
      !!this.appliedSearch() ||
      this.status() !== null ||
      this.customerId() !== null ||
      this.warehouseId() !== null,
  );

  ngOnInit(): void {
    merge(
      this.searchInput$.pipe(
        debounceTime(300),
        map((value) => value.trim()),
        distinctUntilChanged(),
        filter((value) => value !== this.appliedSearch()),
        tap((value) => {
          this.appliedSearch.set(value);
          this.page.set(1);
        }),
      ),
      this.reload$,
    )
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.ordersService
            .listSalesOrders({
              search: this.appliedSearch(),
              status: this.status(),
              customerId: this.customerId(),
              warehouseId: this.warehouseId(),
              pageNumber: this.page(),
              pageSize: this.pageSize,
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load Sales Orders. Please try again.');
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        this.orders.set(result?.data ?? []);
        this.totalCount.set(result?.totalCount ?? 0);
        if (result && this.page() > this.pageCount()) {
          this.page.set(this.pageCount());
          this.reload$.next();
        }
      });

    this.loadCustomers();
    if (this.canListWarehouses) this.loadWarehouses();
    this.reload$.next();
  }

  loadCustomers(): void {
    this.customersLoading.set(true);
    this.customersError.set(false);
    this.customersService
      .list({ searchTerm: '', isActive: null, pageNumber: 1, pageSize: 100 })
      .pipe(
        expand((result) =>
          result.pageNumber * result.pageSize < result.totalCount
            ? this.customersService.list({
                searchTerm: '',
                isActive: null,
                pageNumber: result.pageNumber + 1,
                pageSize: 100,
              })
            : EMPTY,
        ),
        reduce((items, result) => items.concat(result.data), [] as Customer[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (items) => {
          this.customers.set(items);
          this.customersLoading.set(false);
        },
        error: () => {
          this.customersLoading.set(false);
          this.customersError.set(true);
        },
      });
  }

  loadWarehouses(): void {
    this.warehousesLoading.set(true);
    this.warehousesError.set(false);
    this.warehousesService
      .list({ search: '', isActive: null, pageNumber: 1, pageSize: 100 })
      .pipe(
        expand((result) =>
          result.pageNumber * result.pageSize < result.totalCount
            ? this.warehousesService.list({
                search: '',
                isActive: null,
                pageNumber: result.pageNumber + 1,
                pageSize: 100,
              })
            : EMPTY,
        ),
        reduce((items, result) => items.concat(result.data), [] as Warehouse[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (items) => {
          this.warehouses.set(items);
          this.warehousesLoading.set(false);
        },
        error: () => {
          this.warehousesLoading.set(false);
          this.warehousesError.set(true);
        },
      });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchInput$.next(value);
  }

  onStatusChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.status.set(value ? (value as SalesOrderStatus) : null);
    this.resetPageAndReload();
  }

  onCustomerChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.customerId.set(value ? Number(value) : null);
    this.resetPageAndReload();
  }

  onWarehouseChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.warehouseId.set(value ? Number(value) : null);
    this.resetPageAndReload();
  }

  goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.pageCount() || page === this.page()) return;
    this.page.set(page);
    this.reload$.next();
  }

  retry(): void {
    this.reload$.next();
  }

  private resetPageAndReload(): void {
    this.appliedSearch.set(this.search().trim());
    this.page.set(1);
    this.reload$.next();
  }
}
