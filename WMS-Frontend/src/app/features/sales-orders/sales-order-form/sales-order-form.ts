import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
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
import {
  catchError,
  distinctUntilChanged,
  map,
  merge,
  of,
  startWith,
  Subject,
  switchMap,
  tap,
  timer,
} from 'rxjs';
import { ProductListItem } from '../../products/models/product';
import { ProductPickerComponent } from '../../purchase-orders/product-picker/product-picker';
import {
  SalesOrderAvailabilityRequest,
  SalesOrderAvailabilityResult,
  SalesOrderLineAvailability,
} from '../models/sales-order';
import { SalesOrdersService } from '../sales-orders.service';

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
  private readonly ordersService = inject(SalesOrdersService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly availabilityRetry$ = new Subject<void>();
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
  readonly availability = signal<SalesOrderAvailabilityResult | null>(null);
  readonly checkingAvailability = signal(false);
  readonly availabilityError = signal<string | null>(null);
  readonly hasShortages = computed(
    () => this.availability()?.lines.some((line) => !line.hasSufficientStock) ?? false,
  );
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
    if (initial) {
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

    merge(
      this.form.controls.warehouseId.valueChanges.pipe(map(() => false)),
      this.lines.valueChanges.pipe(map(() => false)),
      this.availabilityRetry$.pipe(map(() => true)),
    )
      .pipe(
        startWith(false),
        map((retry) => ({
          key: this.availabilityKey(),
          request: this.availabilityRequest(),
          retry,
        })),
        distinctUntilChanged((previous, current) => previous.key === current.key && !current.retry),
        tap(({ request }) => {
          this.availability.set(null);
          this.availabilityError.set(null);
          this.checkingAvailability.set(request !== null);
        }),
        switchMap(({ request }) =>
          request
            ? timer(400).pipe(
                switchMap(() => this.ordersService.checkAvailability(request)),
                catchError((error: HttpErrorResponse) => {
                  this.availabilityError.set(
                    error.status === 401 || error.status === 403
                      ? 'You are not authorized to check stock availability.'
                      : 'Could not check stock availability. You can still save this draft.',
                  );
                  return of(null);
                }),
              )
            : of(null),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.availability.set(result);
        this.checkingAvailability.set(false);
      });
  }

  retryAvailability(): void {
    this.availabilityRetry$.next();
  }

  lineAvailability(line: LineForm): SalesOrderLineAvailability | undefined {
    if (this.availability()?.warehouseId !== this.form.controls.warehouseId.value) return undefined;
    const productId = line.controls.productId.value;
    const quantity = line.controls.quantity.value;
    return this.availability()?.lines.find(
      (item) => item.productId === productId && item.requestedQuantity === quantity,
    );
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
    return quantity !== null &&
      unitPrice !== null &&
      Number.isFinite(quantity) &&
      Number.isFinite(unitPrice)
      ? quantity * unitPrice
      : 0;
  }

  private availabilityKey(): string {
    return JSON.stringify({
      warehouseId: this.form.controls.warehouseId.value,
      lines: this.lines.getRawValue().map(({ productId, quantity }) => ({ productId, quantity })),
    });
  }

  private availabilityRequest(): SalesOrderAvailabilityRequest | null {
    const warehouse = this.form.controls.warehouseId;
    if (!warehouse.valid || warehouse.value === null) return null;

    const counts = new Map<number, number>();
    for (const line of this.lines.controls) {
      const id = line.controls.productId.value;
      if (id !== null) counts.set(id, (counts.get(id) ?? 0) + 1);
    }
    const lines = this.lines.controls.flatMap((line) => {
      const productId = line.controls.productId.value;
      const quantity = line.controls.quantity.value;
      return line.controls.productId.valid &&
        line.controls.quantity.valid &&
        productId !== null &&
        quantity !== null &&
        counts.get(productId) === 1
        ? [{ productId, quantity }]
        : [];
    });
    return lines.length ? { warehouseId: warehouse.value, lines } : null;
  }
}
