import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  input,
  OnInit,
  output,
  signal,
  ViewChild,
} from '@angular/core';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { startWith } from 'rxjs';
import { SalesOrderDetails, SalesOrderPickupResult } from '../models/sales-order';
import { SalesOrdersService } from '../sales-orders.service';

interface PickupRow {
  allocationId: number;
  productSku: string;
  productName: string;
  locationName: string;
  locationBarcode: string | null;
  quantity: number;
  quantityPicked: number;
  remainingQuantity: number;
}

@Component({
  selector: 'app-sales-order-pickup-dialog',
  imports: [DecimalPipe, ReactiveFormsModule],
  templateUrl: './sales-order-pickup-dialog.html',
})
export class SalesOrderPickupDialogComponent implements OnInit {
  private readonly ordersService = inject(SalesOrdersService);
  private readonly destroyRef = inject(DestroyRef);
  private dialogElement?: HTMLElement;

  readonly order = input.required<SalesOrderDetails>();
  readonly close = output<void>();
  readonly completed = output<SalesOrderPickupResult>();
  readonly conflict = output<void>();
  readonly unavailable = output<void>();
  readonly busyChange = output<boolean>();

  readonly rows = signal<PickupRow[]>([]);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly form = new FormGroup({
    quantities: new FormArray<FormControl<number | null>>([]),
    receivedBy: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] }),
    notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
  });
  private readonly formValue = toSignal(this.form.valueChanges.pipe(startWith(this.form.value)), {
    requireSync: true,
  });
  readonly totalRemaining = computed(() =>
    this.rows().reduce((sum, row) => sum + row.remainingQuantity, 0),
  );
  readonly totalPicking = computed(() => {
    this.formValue();
    return this.form.controls.quantities.controls.reduce(
      (sum, control) => sum + (control.valid && (control.value ?? 0) > 0 ? control.value! : 0),
      0,
    );
  });
  readonly canSubmit = computed(() => {
    this.formValue();
    return !this.submitting() && this.form.valid && this.totalPicking() > 0;
  });

  @ViewChild('dialog')
  set dialog(element: ElementRef<HTMLElement> | undefined) {
    this.dialogElement = element?.nativeElement;
    element?.nativeElement.focus();
  }

  ngOnInit(): void {
    const rows = this.order().lines.flatMap((line) =>
      line.allocations
        .filter((allocation) => allocation.status === 'Active' && allocation.remainingQuantity > 0)
        .map((allocation) => ({
          allocationId: allocation.id,
          productSku: line.productSku,
          productName: line.productName,
          locationName: allocation.locationName,
          locationBarcode: allocation.locationBarcode,
          quantity: allocation.quantity,
          quantityPicked: allocation.quantityPicked,
          remainingQuantity: allocation.remainingQuantity,
        })),
    );
    this.rows.set(rows);
    for (const row of rows) {
      this.form.controls.quantities.push(
        new FormControl<number | null>(null, this.quantityValidator(row.remainingQuantity)),
      );
    }
  }

  fillAllRemaining(): void {
    if (this.submitting()) return;
    this.rows().forEach((row, index) => {
      this.form.controls.quantities.at(index).setValue(row.remainingQuantity);
    });
  }

  quantityError(index: number): string | null {
    const control = this.form.controls.quantities.at(index);
    if (control.value === null || control.valid) return null;
    if (control.hasError('overRemaining')) {
      return `Cannot pick more than the remaining quantity (${this.rows()[index].remainingQuantity}).`;
    }
    if (control.hasError('precision')) return 'Use at most 16 whole digits and two decimal places.';
    return 'Enter a quantity greater than zero.';
  }

  requestClose(): void {
    if (!this.submitting()) this.close.emit();
  }

  submit(): void {
    if (this.submitting()) return;
    this.form.markAllAsTouched();
    if (!this.canSubmit()) {
      this.error.set(this.form.valid ? 'Enter a pickup quantity for at least one Bin.' : null);
      return;
    }

    const lines = this.rows().flatMap((row, index) => {
      const quantity = this.form.controls.quantities.at(index).value;
      return quantity !== null && quantity > 0
        ? [{ salesOrderAllocationId: row.allocationId, quantity }]
        : [];
    });
    const receivedBy = this.form.controls.receivedBy.value.trim();
    const notes = this.form.controls.notes.value.trim();
    this.submitting.set(true);
    this.busyChange.emit(true);
    this.error.set(null);
    this.form.disable({ emitEvent: false });
    queueMicrotask(() => this.dialogElement?.focus());
    this.ordersService
      .createSalesOrderPickup(this.order().id, {
        receivedBy: receivedBy || null,
        notes: notes || null,
        lines,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.submitting.set(false);
          this.busyChange.emit(false);
          this.completed.emit(result);
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.busyChange.emit(false);
          if (error.status === 409) {
            this.conflict.emit();
            return;
          }
          if (error.status === 404) {
            this.unavailable.emit();
            return;
          }
          this.form.enable({ emitEvent: false });
          this.error.set(
            error.status === 400
              ? 'Please check the pickup quantities and details.'
              : error.status === 401 || error.status === 403
                ? 'You are not authorized to confirm this pickup.'
                : 'Could not confirm this pickup. Please try again.',
          );
        },
      });
  }

  trapTab(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    const focusable = Array.from(
      this.dialogElement?.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input:not([disabled]), textarea:not([disabled])',
      ) ?? [],
    );
    if (!focusable.length) {
      keyboardEvent.preventDefault();
      this.dialogElement?.focus();
      return;
    }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (this.dialogElement && !this.dialogElement.contains(document.activeElement)) {
      keyboardEvent.preventDefault();
      (keyboardEvent.shiftKey ? last : first).focus();
      return;
    }
    if (
      keyboardEvent.shiftKey &&
      (document.activeElement === first || document.activeElement === this.dialogElement)
    ) {
      keyboardEvent.preventDefault();
      last.focus();
    } else if (!keyboardEvent.shiftKey && document.activeElement === last) {
      keyboardEvent.preventDefault();
      first.focus();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.requestClose();
  }

  private quantityValidator(remainingQuantity: number): ValidatorFn {
    return (control) => {
      const quantity = control.value as number | null;
      if (quantity === null) return null;
      if (typeof quantity !== 'number' || !Number.isFinite(quantity) || quantity <= 0) {
        return { positive: true };
      }
      if (!/^\d{1,16}(?:\.\d{1,2})?$/.test(String(quantity))) {
        return { precision: true };
      }
      return quantity > remainingQuantity ? { overRemaining: true } : null;
    };
  }
}
