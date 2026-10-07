import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { AuthService } from '../../auth/auth.services';
import { SuppliersService } from '../../suppliers/suppliers.service';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { CreatePurchaseOrderRequest } from '../models/purchase-order';
import {
  loadActiveSupplierOptions,
  loadActiveWarehouseOptions,
  PurchaseOrderFormOption,
} from '../purchase-order-form/form-options';
import {
  PurchaseOrderFormComponent,
  PurchaseOrderFormValue,
} from '../purchase-order-form/purchase-order-form';
import { PurchaseOrdersService } from '../purchase-orders.service';

@Component({
  selector: 'app-create-purchase-order-page',
  imports: [PurchaseOrderFormComponent],
  templateUrl: './create-purchase-order-page.html',
})
export class CreatePurchaseOrderPage implements OnInit {
  private readonly ordersService = inject(PurchaseOrdersService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly canListWarehouses = this.auth.hasRole('TenantAdmin');
  readonly supplierOptions = signal<PurchaseOrderFormOption[]>([]);
  readonly warehouseOptions = signal<PurchaseOrderFormOption[]>([]);
  readonly suppliersLoading = signal(false);
  readonly warehousesLoading = signal(false);
  readonly suppliersError = signal(false);
  readonly warehousesError = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadSuppliers();
    if (this.canListWarehouses) this.loadWarehouses();
  }

  loadSuppliers(): void {
    this.suppliersLoading.set(true);
    this.suppliersError.set(false);
    loadActiveSupplierOptions(this.suppliersService)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.supplierOptions.set(items);
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
          this.warehouseOptions.set(items);
          this.warehousesLoading.set(false);
        },
        error: () => {
          this.warehousesLoading.set(false);
          this.warehousesError.set(true);
        },
      });
  }

  submit(value: PurchaseOrderFormValue): void {
    if (this.submitting()) return;
    const request: CreatePurchaseOrderRequest = value;
    this.submitError.set(null);
    this.submitting.set(true);
    this.ordersService
      .createPurchaseOrder(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (id) => {
          if (!Number.isSafeInteger(id) || id <= 0) {
            this.failSubmission(
              'Draft was saved, but the response did not contain a valid Purchase Order ID.',
            );
            return;
          }
          void this.router.navigate(['/purchase-orders', id]).then((navigated) => {
            if (!navigated)
              this.failSubmission('Draft was saved, but its details could not be opened.');
          });
        },
        error: (error: HttpErrorResponse) => {
          const message =
            error.status === 409
              ? 'The Purchase Order could not be created because of a conflict. Please try again.'
              : error.status === 404
                ? 'A selected supplier, warehouse, or product is no longer active or available. Review your selections and try again.'
                : error.status === 400
                  ? 'Please check the Purchase Order details and line values.'
                  : error.status === 401 || error.status === 403
                    ? 'You are not authorized to create Purchase Orders.'
                    : 'Could not save the draft Purchase Order. Please try again.';
          this.failSubmission(message);
        },
      });
  }

  cancel(): void {
    if (!this.submitting()) void this.router.navigateByUrl('/purchase-orders');
  }

  private failSubmission(message: string): void {
    this.submitting.set(false);
    this.submitError.set(message);
  }
}
