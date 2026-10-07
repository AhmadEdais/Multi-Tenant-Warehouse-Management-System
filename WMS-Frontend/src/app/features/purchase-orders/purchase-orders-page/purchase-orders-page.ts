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
  map,
  merge,
  of,
  reduce,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { AuthService } from '../../auth/auth.services';
import { Supplier } from '../../suppliers/models/supplier';
import { SuppliersService } from '../../suppliers/suppliers.service';
import { Warehouse } from '../../warehouses/models/warehouse';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { PurchaseOrderListItem, PurchaseOrderStatus } from '../models/purchase-order';
import { PurchaseOrderStatusBadge } from '../purchase-order-status-badge/purchase-order-status-badge';
import { PurchaseOrdersService } from '../purchase-orders.service';
import { MANAGE_INBOUND_ROLES } from '../permissions';

@Component({
  selector: 'app-purchase-orders-page',
  imports: [DatePipe, DecimalPipe, RouterLink, PurchaseOrderStatusBadge],
  templateUrl: './purchase-orders-page.html',
})
export class PurchaseOrdersPage implements OnInit {
  private readonly ordersService = inject(PurchaseOrdersService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  readonly canListWarehouses = this.auth.hasRole('TenantAdmin');
  readonly canCreate = computed(
    () => !this.auth.hasRole('SystemAdmin') && this.auth.hasAnyRole(MANAGE_INBOUND_ROLES),
  );
  readonly pageSize = 20;
  readonly orders = signal<PurchaseOrderListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly search = signal('');
  readonly appliedSearch = signal('');
  readonly status = signal<PurchaseOrderStatus | null>(null);
  readonly supplierId = signal<number | null>(null);
  readonly warehouseId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly suppliers = signal<Supplier[]>([]);
  readonly suppliersLoading = signal(false);
  readonly suppliersError = signal(false);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly warehousesLoading = signal(false);
  readonly warehousesError = signal(false);
  readonly warehouseInputError = signal<string | null>(null);

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly firstItem = computed(() =>
    this.totalCount() ? (this.page() - 1) * this.pageSize + 1 : 0,
  );
  readonly lastItem = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));
  readonly hasFilters = computed(
    () =>
      !!this.appliedSearch() ||
      this.status() !== null ||
      this.supplierId() !== null ||
      this.warehouseId() !== null,
  );

  ngOnInit(): void {
    merge(
      this.searchInput$.pipe(
        debounceTime(300),
        map((value) => value.trim()),
        distinctUntilChanged(),
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
            .listPurchaseOrders({
              search: this.appliedSearch(),
              status: this.status(),
              supplierId: this.supplierId(),
              warehouseId: this.warehouseId(),
              pageNumber: this.page(),
              pageSize: this.pageSize,
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load Purchase Orders. Please try again.');
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

    this.loadSuppliers();
    if (this.canListWarehouses) this.loadWarehouses();
    this.reload$.next();
  }

  loadSuppliers(): void {
    this.suppliersLoading.set(true);
    this.suppliersError.set(false);
    this.suppliersService
      .list({ searchTerm: '', isActive: null, pageNumber: 1, pageSize: 100 })
      .pipe(
        expand((result) =>
          result.pageNumber * result.pageSize < result.totalCount
            ? this.suppliersService.list({
                searchTerm: '',
                isActive: null,
                pageNumber: result.pageNumber + 1,
                pageSize: 100,
              })
            : EMPTY,
        ),
        reduce((items, result) => items.concat(result.data), [] as Supplier[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (items) => {
          this.suppliers.set(items);
          this.suppliersLoading.set(false);
        },
        error: () => {
          this.suppliersLoading.set(false);
          this.suppliersError.set(true);
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
    this.status.set(value ? (value as PurchaseOrderStatus) : null);
    this.resetPageAndReload();
  }

  onSupplierChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.supplierId.set(value ? Number(value) : null);
    this.resetPageAndReload();
  }

  onWarehouseChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.warehouseId.set(value ? Number(value) : null);
    this.resetPageAndReload();
  }

  onWarehouseIdChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value.trim();
    const id = Number(value);
    if (value && (!Number.isInteger(id) || id <= 0)) {
      this.warehouseInputError.set('Enter a positive Warehouse ID.');
      return;
    }
    this.warehouseInputError.set(null);
    this.warehouseId.set(value ? id : null);
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
    this.page.set(1);
    this.reload$.next();
  }
}
