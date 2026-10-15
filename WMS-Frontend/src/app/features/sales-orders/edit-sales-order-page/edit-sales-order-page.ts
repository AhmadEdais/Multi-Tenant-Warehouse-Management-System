import { PageHeaderComponent } from '../../../shared/components/page-header/page-header';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, combineLatest, distinctUntilChanged, map, of, startWith, Subject, switchMap } from 'rxjs';
import { CustomersService } from '../../customers/customers.service';
import { loadActiveWarehouseOptions } from '../../purchase-orders/purchase-order-form/form-options';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { SalesOrderDetails, UpdateSalesOrderRequest } from '../models/sales-order';
import { loadActiveCustomerOptions } from '../sales-order-form/form-options';
import {
  SalesOrderFormComponent,
  SalesOrderFormInitialValue,
  SalesOrderFormOption,
  SalesOrderFormValue,
} from '../sales-order-form/sales-order-form';
import { SalesOrdersService } from '../sales-orders.service';

@Component({
  selector: 'app-edit-sales-order-page',
  imports: [RouterLink, SalesOrderFormComponent, PageHeaderComponent],
  templateUrl: './edit-sales-order-page.html',
})
export class EditSalesOrderPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ordersService = inject(SalesOrdersService);
  private readonly customersService = inject(CustomersService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();

  readonly order = signal<SalesOrderDetails | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly activeCustomerOptions = signal<SalesOrderFormOption[]>([]);
  readonly activeWarehouseOptions = signal<SalesOrderFormOption[]>([]);
  readonly customersLoading = signal(false);
  readonly warehousesLoading = signal(false);
  readonly customersError = signal(false);
  readonly warehousesError = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly saved = signal(false);
  readonly navigationFailed = signal(false);

  readonly initialValue = computed<SalesOrderFormInitialValue | null>(() => {
    const order = this.order();
    if (!order || order.status !== 'Draft') return null;
    return {
      customerId: order.customerId,
      warehouseId: order.warehouseId,
      requestedShipmentDate: order.requestedShipmentDate?.slice(0, 10) ?? null,
      lines: order.lines.map((line) => ({
        productId: line.productId,
        productSku: line.productSku,
        productName: line.productName,
        quantity: line.quantity,
        unitPrice: line.unitPrice,
      })),
    };
  });
  readonly customerOptions = computed(() => {
    const options = this.activeCustomerOptions();
    const order = this.order();
    if (!order || options.some((item) => item.id === order.customerId)) return options;
    const unavailable = !this.customersLoading() && !this.customersError();
    return [{
      id: order.customerId,
      label: `${order.customerCode} — ${order.customerName}${unavailable ? ' (unavailable — select an active customer)' : ''}`,
    }, ...options];
  });
  readonly warehouseOptions = computed(() => {
    const options = this.activeWarehouseOptions();
    const order = this.order();
    if (!order || options.some((item) => item.id === order.warehouseId)) return options;
    const unavailable = !this.warehousesLoading() && !this.warehousesError();
    return [{
      id: order.warehouseId,
      label: `${order.warehouseCode} — ${order.warehouseName}${unavailable ? ' (unavailable — select an active warehouse)' : ''}`,
    }, ...options];
  });

  ngOnInit(): void {
    combineLatest([
      this.route.paramMap.pipe(
        map((params) => params.get('id')),
        distinctUntilChanged(),
      ),
      this.reload$.pipe(startWith(undefined)),
    ])
      .pipe(
        switchMap(([rawId]) => {
          this.loading.set(true);
          this.order.set(null);
          this.notFound.set(false);
          this.error.set(null);
          this.submitError.set(null);
          this.saved.set(false);
          this.navigationFailed.set(false);
          if (!rawId || !/^\d+$/.test(rawId) || !Number.isSafeInteger(Number(rawId)) || Number(rawId) <= 0) {
            this.notFound.set(true);
            return of(null);
          }
          return this.ordersService.getSalesOrderById(Number(rawId)).pipe(
            catchError((error: HttpErrorResponse) => {
              if (error.status === 404) this.notFound.set(true);
              else if (error.status === 401 || error.status === 403)
                this.error.set('You are not authorized to edit this Sales Order.');
              else this.error.set('Could not load this Sales Order. Please try again.');
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
          this.loadCustomers();
          this.loadWarehouses();
        }
      });
  }

  retry(): void {
    this.reload$.next();
  }

  loadCustomers(): void {
    this.customersLoading.set(true);
    this.customersError.set(false);
    loadActiveCustomerOptions(this.customersService)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.activeCustomerOptions.set(items);
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

  submit(value: SalesOrderFormValue): void {
    const order = this.order();
    if (!order || order.status !== 'Draft' || this.submitting() || this.saved()) return;
    if (!this.activeCustomerOptions().some((item) => item.id === value.customerId) ||
        !this.activeWarehouseOptions().some((item) => item.id === value.warehouseId)) {
      this.submitError.set('Select an active customer and warehouse before saving.');
      return;
    }

    const request: UpdateSalesOrderRequest = value;
    this.submitError.set(null);
    this.submitting.set(true);
    this.ordersService.updateSalesOrder(order.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saved.set(true);
          this.submitting.set(false);
          void this.router.navigate(['/sales-orders', order.id])
            .then((navigated) => {
              if (!navigated) this.navigationFailed.set(true);
            })
            .catch(() => this.navigationFailed.set(true));
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.submitError.set(
            error.status === 409
              ? 'This Sales Order changed or is no longer a Draft. Return to its details and refresh before trying again.'
              : error.status === 404
                ? 'The Sales Order or a selected customer, warehouse, or product is no longer available.'
                : error.status === 400
                  ? 'Please check the Sales Order details and line values.'
                  : error.status === 401 || error.status === 403
                    ? 'You are not authorized to edit Sales Orders.'
                    : 'Could not save the Sales Order. Please try again.',
          );
        },
      });
  }

  cancel(): void {
    const id = this.order()?.id;
    if (!this.submitting() && id !== undefined) void this.router.navigate(['/sales-orders', id]);
  }
}
