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
import { ProductPickerComponent } from '../product-picker/product-picker';
import { PurchaseOrderFormOption } from './form-options';

interface PurchaseOrderFormLineValue {
  productId: number;
  expectedQuantity: number;
  unitCost: number;
}

export interface PurchaseOrderFormValue {
  supplierId: number;
  warehouseId: number;
  expectedDeliveryDate: string | null;
  lines: PurchaseOrderFormLineValue[];
}

export interface PurchaseOrderFormInitialValue extends PurchaseOrderFormValue {
  lines: (PurchaseOrderFormLineValue & { productSku: string; productName: string })[];
}

type SelectedProduct = { id: number; sku: string; name: string };
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

const newLine = (value?: PurchaseOrderFormLineValue): LineForm =>
  new FormGroup({
    productId: new FormControl<number | null>(value?.productId ?? null, [
      Validators.required,
      positiveId,
    ]),
    expectedQuantity: new FormControl<number | null>(value?.expectedQuantity ?? null, [
      Validators.required,
      Validators.min(0.01),
      decimal18_2,
    ]),
    unitCost: new FormControl<number | null>(value?.unitCost ?? null, [
      Validators.required,
      Validators.min(0),
      decimal18_2,
    ]),
  });

@Component({
  selector: 'app-purchase-order-form',
  imports: [ReactiveFormsModule, DecimalPipe, ProductPickerComponent],
  templateUrl: './purchase-order-form.html',
})
export class PurchaseOrderFormComponent implements OnInit {
  readonly mode = input<'create' | 'edit'>('create');
  readonly orderNumber = input<string | null>(null);
  readonly initialValue = input<PurchaseOrderFormInitialValue | null>(null);
  readonly supplierOptions = input<PurchaseOrderFormOption[]>([]);
  readonly warehouseOptions = input<PurchaseOrderFormOption[]>([]);
  readonly suppliersLoading = input(false);
  readonly warehousesLoading = input(false);
  readonly suppliersError = input(false);
  readonly warehousesError = input(false);
  readonly submitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly save = output<PurchaseOrderFormValue>();
  readonly cancel = output<void>();
  readonly retrySuppliers = output<void>();
  readonly retryWarehouses = output<void>();

  readonly selectedProducts = signal(new Map<LineForm, SelectedProduct>());
  readonly selectedLine = signal<LineForm | null>(null);
  private pickerTrigger: HTMLElement | null = null;

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
      supplierId: initial.supplierId,
      warehouseId: initial.warehouseId,
      expectedDeliveryDate: initial.expectedDeliveryDate ?? '',
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
    if (
      this.lines.controls.some(
        (other) => other !== line && other.controls.productId.value === product.id,
      )
    )
      return;

    if (line.controls.productId.value !== product.id) {
      line.controls.productId.setValue(product.id);
      line.controls.unitCost.setValue(Math.round((product.unitCost + Number.EPSILON) * 100) / 100);
    }
    this.selectedProducts.update((products) =>
      new Map(products).set(line, { id: product.id, sku: product.sku, name: product.name }),
    );
    this.closeProductPicker();
  }

  clearProduct(line: LineForm): void {
    if (this.submitting()) return;
    line.controls.productId.setValue(null);
    line.controls.unitCost.setValue(null);
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
    this.form.controls.expectedDeliveryDate.updateValueAndValidity();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.lines.markAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    this.save.emit({
      supplierId: values.supplierId!,
      warehouseId: values.warehouseId!,
      expectedDeliveryDate: values.expectedDeliveryDate || null,
      lines: values.lines.map((line) => ({
        productId: line.productId!,
        expectedQuantity: line.expectedQuantity!,
        unitCost: line.unitCost!,
      })),
    });
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
