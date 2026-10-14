import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { CustomersService } from '../../customers/customers.service';
import { loadActiveWarehouseOptions } from '../../purchase-orders/purchase-order-form/form-options';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { CreateSalesOrderRequest } from '../models/sales-order';
import { loadActiveCustomerOptions } from '../sales-order-form/form-options';
import { SalesOrderFormComponent, SalesOrderFormOption, SalesOrderFormValue } from '../sales-order-form/sales-order-form';
import { SalesOrdersService } from '../sales-orders.service';

@Component({
  selector: 'app-create-sales-order-page',
  imports: [RouterLink, SalesOrderFormComponent],
  templateUrl: './create-sales-order-page.html',
})
export class CreateSalesOrderPage implements OnInit {
  private readonly ordersService = inject(SalesOrdersService);
  private readonly customersService = inject(CustomersService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly customerOptions = signal<SalesOrderFormOption[]>([]);
  readonly warehouseOptions = signal<SalesOrderFormOption[]>([]);
  readonly customersLoading = signal(false);
  readonly warehousesLoading = signal(false);
  readonly customersError = signal(false);
  readonly warehousesError = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly savedOrderId = signal<number | null>(null);
  readonly savedWithoutId = signal(false);
  readonly navigationFailed = signal(false);

  ngOnInit(): void {
    this.loadCustomers();
    this.loadWarehouses();
  }

  loadCustomers(): void {
    this.customersLoading.set(true);
    this.customersError.set(false);
    loadActiveCustomerOptions(this.customersService)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.customerOptions.set(items);
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
          this.warehouseOptions.set(items);
          this.warehousesLoading.set(false);
        },
        error: () => {
          this.warehousesLoading.set(false);
          this.warehousesError.set(true);
        },
      });
  }

  submit(value: SalesOrderFormValue): void {
    if (this.submitting() || this.savedOrderId() !== null || this.savedWithoutId()) return;
    const request: CreateSalesOrderRequest = value;
    this.submitError.set(null);
    this.submitting.set(true);
    this.ordersService.createSalesOrder(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (id) => {
          if (!Number.isSafeInteger(id) || id <= 0) {
            this.savedWithoutId.set(true);
            this.submitting.set(false);
            return;
          }
          this.savedOrderId.set(id);
          this.submitting.set(false);
          void this.router.navigate(['/sales-orders', id], { state: { salesOrderCreated: true } })
            .then((navigated) => {
              if (!navigated) this.navigationFailed.set(true);
            })
            .catch(() => this.navigationFailed.set(true));
        },
        error: (error: HttpErrorResponse) => {
          const message = error.status === 400
            ? 'Please check the Sales Order details and line values.'
            : error.status === 404
              ? 'A selected customer, warehouse, or product is no longer active or available. Review your selections and try again.'
              : error.status === 409
                ? 'The Sales Order could not be created because of a conflict. Please try again.'
                : error.status === 401 || error.status === 403
                  ? 'You are not authorized to create Sales Orders.'
                  : 'Could not save the draft Sales Order. Please try again.';
          this.failSubmission(message);
        },
      });
  }

  cancel(): void {
    if (!this.submitting()) void this.router.navigateByUrl('/sales-orders');
  }

  private failSubmission(message: string): void {
    this.submitting.set(false);
    this.submitError.set(message);
  }
}
