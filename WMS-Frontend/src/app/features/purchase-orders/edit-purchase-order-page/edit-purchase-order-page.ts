import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  catchError,
  combineLatest,
  distinctUntilChanged,
  map,
  of,
  startWith,
  Subject,
  switchMap,
} from 'rxjs';
import { AuthService } from '../../auth/auth.services';
import { SuppliersService } from '../../suppliers/suppliers.service';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { PurchaseOrderDetails, UpdatePurchaseOrderRequest } from '../models/purchase-order';
import {
  loadActiveSupplierOptions,
  loadActiveWarehouseOptions,
  PurchaseOrderFormOption,
} from '../purchase-order-form/form-options';
import {
  PurchaseOrderFormComponent,
  PurchaseOrderFormInitialValue,
  PurchaseOrderFormValue,
} from '../purchase-order-form/purchase-order-form';
import { PurchaseOrdersService } from '../purchase-orders.service';

@Component({
  selector: 'app-edit-purchase-order-page',
  imports: [RouterLink, PurchaseOrderFormComponent],
  templateUrl: './edit-purchase-order-page.html',
})
export class EditPurchaseOrderPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ordersService = inject(PurchaseOrdersService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();

  readonly canListWarehouses = this.auth.hasRole('TenantAdmin');
  readonly order = signal<PurchaseOrderDetails | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly activeSupplierOptions = signal<PurchaseOrderFormOption[]>([]);
  readonly activeWarehouseOptions = signal<PurchaseOrderFormOption[]>([]);
  readonly suppliersLoading = signal(false);
  readonly warehousesLoading = signal(false);
  readonly suppliersError = signal(false);
  readonly warehousesError = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly initialValue = computed<PurchaseOrderFormInitialValue | null>(() => {
    const order = this.order();
    if (!order || order.status !== 'Draft') return null;
    return {
      supplierId: order.supplierId,
      warehouseId: order.warehouseId,
      expectedDeliveryDate: order.expectedDeliveryDate?.slice(0, 10) ?? null,
      lines: order.lines.map((line) => ({
        productId: line.productId,
        productSku: line.productSku,
        productName: line.productName,
        expectedQuantity: line.expectedQuantity,
        unitCost: line.unitCost,
      })),
    };
  });
  readonly supplierOptions = computed(() => {
    const options = this.activeSupplierOptions();
    const order = this.order();
    return order && !options.some((item) => item.id === order.supplierId)
      ? [
          { id: order.supplierId, label: `${order.supplierCode} — ${order.supplierName}` },
          ...options,
        ]
      : options;
  });
  readonly warehouseOptions = computed(() => {
    const options = this.activeWarehouseOptions();
    const order = this.order();
    return order && !options.some((item) => item.id === order.warehouseId)
      ? [
          { id: order.warehouseId, label: `${order.warehouseCode} — ${order.warehouseName}` },
          ...options,
        ]
      : options;
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(
        map((params) => Number(params.get('id'))),
        distinctUntilChanged(),
      ),
      this.reload$.pipe(startWith(undefined)),
    ])
      .pipe(
        switchMap(([id]) => {
          this.loading.set(true);
          this.order.set(null);
          this.notFound.set(false);
          this.error.set(null);
          this.submitError.set(null);
          if (!Number.isSafeInteger(id) || id <= 0) {
            this.notFound.set(true);
            return of(null);
          }
          return this.ordersService.getPurchaseOrderById(id).pipe(
            catchError((error: HttpErrorResponse) => {
              if (error.status === 404) this.notFound.set(true);
              else this.error.set('Could not load this Purchase Order. Please try again.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((order) => {
        this.loading.set(false);
        this.order.set(order);
        if (order?.status === 'Draft') {
          this.loadSuppliers();
          if (this.canListWarehouses) this.loadWarehouses();
        }
      });
  }

  retry(): void {
    this.reload$.next();
  }

  loadSuppliers(): void {
    this.suppliersLoading.set(true);
    this.suppliersError.set(false);
    loadActiveSupplierOptions(this.suppliersService)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.activeSupplierOptions.set(items);
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
    loadActiveWarehouseOptions(this.warehousesService)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.activeWarehouseOptions.set(items);
          this.warehousesLoading.set(false);
        },
        error: () => {
          this.warehousesLoading.set(false);
          this.warehousesError.set(true);
        },
      });
  }

  submit(value: PurchaseOrderFormValue): void {
    const order = this.order();
    if (!order || order.status !== 'Draft' || this.submitting()) return;
    const request: UpdatePurchaseOrderRequest = value;
    this.submitError.set(null);
    this.submitting.set(true);
    this.ordersService
      .updatePurchaseOrder(order.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          void this.router.navigate(['/purchase-orders', order.id]).then((navigated) => {
            if (!navigated) {
              this.submitting.set(false);
              this.submitError.set(
                'Changes were saved, but the Purchase Order details could not be opened.',
              );
            }
          });
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.submitError.set(
            error.status === 409
              ? 'This Purchase Order changed or is no longer a Draft. Return to its details and refresh before trying again.'
              : error.status === 404
                ? 'The Purchase Order or a selected supplier, warehouse, or product is no longer available.'
                : error.status === 400
                  ? 'Please check the Purchase Order details and line values.'
                  : error.status === 401 || error.status === 403
                    ? 'You are not authorized to edit Purchase Orders.'
                    : 'Could not save the Purchase Order. Please try again.',
          );
        },
      });
  }

  cancel(): void {
    const id = this.order()?.id;
    if (!this.submitting() && id !== undefined) void this.router.navigate(['/purchase-orders', id]);
  }
}
