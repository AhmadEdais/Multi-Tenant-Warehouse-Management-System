import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { EMPTY, expand, reduce, startWith } from 'rxjs';
import { AuthService } from '../../auth/auth.services';
import { ProductListItem } from '../../products/models/product';
import { ProductsService } from '../../products/products.service';
import { Supplier } from '../../suppliers/models/supplier';
import { SuppliersService } from '../../suppliers/suppliers.service';
import { Warehouse } from '../../warehouses/models/warehouse';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { CreatePurchaseOrderRequest } from '../models/purchase-order';
import { PurchaseOrdersService } from '../purchase-orders.service';

type LineForm = FormGroup<{
  productId: FormControl<number | null>;
  expectedQuantity: FormControl<number | null>;
  unitCost: FormControl<number | null>;
}>;

const positiveId = (control: AbstractControl): ValidationErrors | null =>
  control.value === null || (Number.isInteger(control.value) && control.value > 0)
    ? null
    : { positiveId: true };

const decimal18_2 = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value as number | null;
  return value === null ||
    (Number.isFinite(value) && /^\d{1,16}(?:\.\d{1,2})?$/.test(String(value)))
    ? null
    : { decimal18_2: true };
};

const localToday = (): string => {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
};

const expectedDate = (control: AbstractControl): ValidationErrors | null =>
  !control.value || (typeof control.value === 'string' && control.value >= localToday())
    ? null
    : { pastDate: true };

const validLines = (control: AbstractControl): ValidationErrors | null => {
  const lines = control as FormArray<LineForm>;
  if (lines.length === 0) return { required: true };
  const selected = lines.controls
    .map((line) => line.controls.productId.value)
    .filter((id) => id !== null);
  return new Set(selected).size === selected.length ? null : { duplicateProducts: true };
};

const newLine = (): LineForm =>
  new FormGroup({
    productId: new FormControl<number | null>(null, [Validators.required, positiveId]),
    expectedQuantity: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(0.01),
      decimal18_2,
    ]),
    unitCost: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(0),
      decimal18_2,
    ]),
  });

@Component({
  selector: 'app-create-purchase-order-page',
  imports: [ReactiveFormsModule, DecimalPipe],
  templateUrl: './create-purchase-order-page.html',
})
export class CreatePurchaseOrderPage implements OnInit {
  private readonly ordersService = inject(PurchaseOrdersService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly productsService = inject(ProductsService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly canListWarehouses = this.auth.hasRole('TenantAdmin');
  readonly suppliers = signal<Supplier[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly products = signal<ProductListItem[]>([]);
  readonly suppliersLoading = signal(false);
  readonly warehousesLoading = signal(false);
  readonly productsLoading = signal(false);
  readonly suppliersError = signal(false);
  readonly warehousesError = signal(false);
  readonly productsError = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly form = new FormGroup({
    supplierId: new FormControl<number | null>(null, [Validators.required, positiveId]),
    warehouseId: new FormControl<number | null>(null, [Validators.required, positiveId]),
    expectedDeliveryDate: new FormControl('', { nonNullable: true, validators: [expectedDate] }),
    lines: new FormArray<LineForm>([newLine()], { validators: [validLines] }),
  });
  readonly lines = this.form.controls.lines;
  private readonly formValue = toSignal(
    this.form.valueChanges.pipe(startWith(this.form.getRawValue())),
    { requireSync: true },
  );
  readonly poTotal = computed(() => {
    this.formValue();
    return this.lines.controls.reduce((total, line) => total + this.calculateLineTotal(line), 0);
  });

  ngOnInit(): void {
    this.loadSuppliers();
    if (this.canListWarehouses) this.loadWarehouses();
    this.loadProducts();
  }

  loadSuppliers(): void {
    this.suppliersLoading.set(true);
    this.suppliersError.set(false);
    this.suppliersService
      .list({ searchTerm: '', isActive: true, pageNumber: 1, pageSize: 100 })
      .pipe(
        expand((page) =>
          page.pageNumber * page.pageSize < page.totalCount
            ? this.suppliersService.list({
                searchTerm: '',
                isActive: true,
                pageNumber: page.pageNumber + 1,
                pageSize: 100,
              })
            : EMPTY,
        ),
        reduce((items, page) => items.concat(page.data), [] as Supplier[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (items) => {
          this.suppliers.set(items.filter((item) => item.isActive));
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
      .list({ search: '', isActive: true, pageNumber: 1, pageSize: 100 })
      .pipe(
        expand((page) =>
          page.pageNumber * page.pageSize < page.totalCount
            ? this.warehousesService.list({
                search: '',
                isActive: true,
                pageNumber: page.pageNumber + 1,
                pageSize: 100,
              })
            : EMPTY,
        ),
        reduce((items, page) => items.concat(page.data), [] as Warehouse[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (items) => {
          this.warehouses.set(items.filter((item) => item.isActive));
          this.warehousesLoading.set(false);
        },
        error: () => {
          this.warehousesLoading.set(false);
          this.warehousesError.set(true);
        },
      });
  }

  loadProducts(): void {
    this.productsLoading.set(true);
    this.productsError.set(false);
    this.productsService
      .list({ searchTerm: '', categoryId: null, isActive: true, pageNumber: 1, pageSize: 100 })
      .pipe(
        expand((page) =>
          page.pageNumber * page.pageSize < page.totalCount
            ? this.productsService.list({
                searchTerm: '',
                categoryId: null,
                isActive: true,
                pageNumber: page.pageNumber + 1,
                pageSize: 100,
              })
            : EMPTY,
        ),
        reduce((items, page) => items.concat(page.data), [] as ProductListItem[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (items) => {
          this.products.set(items.filter((item) => item.isActive));
          this.productsLoading.set(false);
        },
        error: () => {
          this.productsLoading.set(false);
          this.productsError.set(true);
        },
      });
  }

  addLine(): void {
    if (!this.submitting()) this.lines.push(newLine());
  }

  removeLine(index: number): void {
    if (!this.submitting() && this.lines.length > 1) this.lines.removeAt(index);
  }

  productSelectedElsewhere(productId: number, rowIndex: number): boolean {
    return this.lines.controls.some(
      (line, index) => index !== rowIndex && line.controls.productId.value === productId,
    );
  }

  onProductChanged(index: number): void {
    const line = this.lines.at(index);
    const product = this.products().find((item) => item.id === line.controls.productId.value);
    line.controls.unitCost.setValue(
      product ? Math.round((product.unitCost + Number.EPSILON) * 100) / 100 : null,
    );
  }

  lineTotal(index: number): number {
    this.formValue();
    return this.calculateLineTotal(this.lines.at(index));
  }

  submit(): void {
    if (this.submitting()) return;
    this.form.controls.expectedDeliveryDate.updateValueAndValidity();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.lines.markAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const request: CreatePurchaseOrderRequest = {
      supplierId: values.supplierId!,
      warehouseId: values.warehouseId!,
      expectedDeliveryDate: values.expectedDeliveryDate || null,
      lines: values.lines.map((line) => ({
        productId: line.productId!,
        expectedQuantity: line.expectedQuantity!,
        unitCost: line.unitCost!,
      })),
    };

    this.submitError.set(null);
    this.submitting.set(true);
    this.form.disable({ emitEvent: false });
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
    this.form.enable({ emitEvent: false });
    this.submitError.set(message);
  }

  private calculateLineTotal(line: LineForm): number {
    const { expectedQuantity, unitCost } = line.getRawValue();
    return expectedQuantity !== null &&
      unitCost !== null &&
      Number.isFinite(expectedQuantity) &&
      Number.isFinite(unitCost)
      ? expectedQuantity * unitCost
      : 0;
  }
}
