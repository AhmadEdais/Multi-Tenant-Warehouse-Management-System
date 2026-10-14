import { DecimalPipe } from '@angular/common';
import { Component, computed, effect, input, OnInit, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { startWith } from 'rxjs';
import { ProductListItem } from '../../products/models/product';
import { ProductPickerComponent } from '../../purchase-orders/product-picker/product-picker';

export interface SalesOrderFormOption {
  id: number;
  label: string;
}

interface SalesOrderFormLineValue {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface SalesOrderFormValue {
  customerId: number;
  warehouseId: number;
  requestedShipmentDate: string | null;
  lines: SalesOrderFormLineValue[];
}

export interface SalesOrderFormInitialValue extends SalesOrderFormValue {
  lines: (SalesOrderFormLineValue & { productSku: string; productName: string })[];
}

type SelectedProduct = { id: number; sku: string; name: string };
type LineForm = FormGroup<{
  productId: FormControl<number | null>;
  quantity: FormControl<number | null>;
  unitPrice: FormControl<number | null>;
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

const decimal18_4 = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value as number | null;
  return value === null ||
    (Number.isFinite(value) && /^\d{1,14}(?:\.\d{1,4})?$/.test(String(value)))
    ? null
    : { decimal18_4: true };
};

const localToday = (): string => {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
};

const requestedDate = (control: AbstractControl): ValidationErrors | null =>
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

const newLine = (value?: SalesOrderFormLineValue): LineForm =>
  new FormGroup({
    productId: new FormControl<number | null>(value?.productId ?? null, [
      Validators.required,
      positiveId,
    ]),
    quantity: new FormControl<number | null>(value?.quantity ?? null, [
      Validators.required,
      Validators.min(0.01),
      decimal18_2,
    ]),
    unitPrice: new FormControl<number | null>(value?.unitPrice ?? null, [
      Validators.required,
      Validators.min(0),
      decimal18_4,
    ]),
  });

@Component({
  selector: 'app-sales-order-form',
  imports: [ReactiveFormsModule, DecimalPipe, ProductPickerComponent],
  templateUrl: './sales-order-form.html',
})
export class SalesOrderFormComponent implements OnInit {
  readonly mode = input<'create' | 'edit'>('create');
  readonly orderNumber = input<string | null>(null);
  readonly initialValue = input<SalesOrderFormInitialValue | null>(null);
  readonly customerOptions = input<SalesOrderFormOption[]>([]);
  readonly warehouseOptions = input<SalesOrderFormOption[]>([]);
  readonly customersLoading = input(false);
  readonly warehousesLoading = input(false);
  readonly customersError = input(false);
  readonly warehousesError = input(false);
  readonly submitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly save = output<SalesOrderFormValue>();
  readonly cancel = output<void>();
  readonly retryCustomers = output<void>();
  readonly retryWarehouses = output<void>();

  readonly selectedProducts = signal(new Map<LineForm, SelectedProduct>());
  readonly selectedLine = signal<LineForm | null>(null);
  private pickerTrigger: HTMLElement | null = null;

  readonly form = new FormGroup({
    customerId: new FormControl<number | null>(null, [Validators.required, positiveId]),
    warehouseId: new FormControl<number | null>(null, [Validators.required, positiveId]),
    requestedShipmentDate: new FormControl('', {
      nonNullable: true,
      validators: [requestedDate],
    }),
    lines: new FormArray<LineForm>([newLine()], { validators: [validLines] }),
  });
  readonly lines = this.form.controls.lines;
  private readonly formValue = toSignal(
    this.form.valueChanges.pipe(startWith(this.form.getRawValue())),
    { requireSync: true },
  );
  readonly totalProductLines = computed(() => {
    this.formValue();
    return this.lines.length;
  });
  readonly totalOrderedQuantity = computed(() => {
    this.formValue();
    return this.lines.controls.reduce((total, line) => {
      const quantity = line.controls.quantity.value;
      return total + (quantity !== null && Number.isFinite(quantity) ? quantity : 0);
    }, 0);
  });
  readonly totalAmount = computed(() => {
    this.formValue();
    return this.lines.controls.reduce((total, line) => total + this.calculateLineTotal(line), 0);
  });
  readonly unavailableProductIds = computed(() =>
    this.lines.controls
      .filter((line) => line !== this.selectedLine())
      .map((line) => line.controls.productId.value)
      .filter((id): id is number => id !== null),
  );

  constructor() {
    effect(() => {
      if (this.submitting()) this.form.disable({ emitEvent: false });
      else this.form.enable({ emitEvent: false });
    });
  }

  ngOnInit(): void {
    const initial = this.initialValue();
    if (!initial) return;

    this.form.patchValue({
      customerId: initial.customerId,
      warehouseId: initial.warehouseId,
      requestedShipmentDate: initial.requestedShipmentDate?.slice(0, 10) ?? '',
    });
    this.lines.clear();
    const selected = new Map<LineForm, SelectedProduct>();
    for (const item of initial.lines) {
      const line = newLine(item);
      this.lines.push(line);
      selected.set(line, { id: item.productId, sku: item.productSku, name: item.productName });
    }
    this.selectedProducts.set(selected);
  }

  addLine(): void {
    if (!this.submitting()) this.lines.push(newLine());
  }

  removeLine(index: number): void {
    if (this.submitting() || this.lines.length === 1) return;
    const line = this.lines.at(index);
    this.lines.removeAt(index);
    this.selectedProducts.update((products) => {
      const next = new Map(products);
      next.delete(line);
      return next;
    });
  }

  selectedProduct(line: LineForm): SelectedProduct | undefined {
    return this.selectedProducts().get(line);
  }

  openProductPicker(line: LineForm, event: Event): void {
    if (this.submitting()) return;
    this.pickerTrigger = event.currentTarget as HTMLElement;
    this.selectedLine.set(line);
  }

  selectProduct(product: ProductListItem): void {
    const line = this.selectedLine();
    if (!line || !this.lines.controls.includes(line) || !product.isActive) return;
    if (this.lines.controls.some(
      (other) => other !== line && other.controls.productId.value === product.id,
    )) return;

    if (line.controls.productId.value !== product.id) {
      line.controls.productId.setValue(product.id);
      line.controls.unitPrice.setValue(product.unitPrice);
    }
    this.selectedProducts.update((products) =>
      new Map(products).set(line, { id: product.id, sku: product.sku, name: product.name }),
    );
    this.closeProductPicker();
  }

  clearProduct(line: LineForm): void {
    if (this.submitting()) return;
    line.controls.productId.setValue(null);
    line.controls.unitPrice.setValue(null);
    this.selectedProducts.update((products) => {
      const next = new Map(products);
      next.delete(line);
      return next;
    });
  }

  closeProductPicker(): void {
    this.selectedLine.set(null);
    queueMicrotask(() => this.pickerTrigger?.focus());
  }

  lineTotal(index: number): number {
    this.formValue();
    return this.calculateLineTotal(this.lines.at(index));
  }

  submit(): void {
    if (this.submitting()) return;
    this.form.controls.requestedShipmentDate.updateValueAndValidity();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.lines.markAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    this.save.emit({
      customerId: values.customerId!,
      warehouseId: values.warehouseId!,
      requestedShipmentDate: values.requestedShipmentDate || null,
      lines: values.lines.map((line) => ({
        productId: line.productId!,
        quantity: line.quantity!,
        unitPrice: line.unitPrice!,
      })),
    });
  }

  private calculateLineTotal(line: LineForm): number {
    const { quantity, unitPrice } = line.getRawValue();
    return quantity !== null && unitPrice !== null &&
      Number.isFinite(quantity) && Number.isFinite(unitPrice)
      ? quantity * unitPrice : 0;
  }
}
