import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, ReactiveFormsModule, ValidatorFn } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
import { PurchaseOrderDetails, PurchaseOrderLineDetails } from '../models/purchase-order';
import { MANAGE_INBOUND_ROLES, RECEIVE_INBOUND_ROLES } from '../permissions';
import { PurchaseOrderStatusBadge } from '../purchase-order-status-badge/purchase-order-status-badge';
import { PurchaseOrdersService } from '../purchase-orders.service';

@Component({
  selector: 'app-purchase-order-details-page',
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, RouterLink, PurchaseOrderStatusBadge],
  templateUrl: './purchase-order-details-page.html',
})
export class PurchaseOrderDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ordersService = inject(PurchaseOrdersService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private cancelTrigger: HTMLElement | null = null;
  private approveTrigger: HTMLElement | null = null;
  private receiveTrigger: HTMLElement | null = null;

  @ViewChild('keepDraftButton')
  set keepDraftButton(button: ElementRef<HTMLButtonElement> | undefined) {
    button?.nativeElement.focus();
  }

  @ViewChild('keepApproveDraftButton')
  set keepApproveDraftButton(button: ElementRef<HTMLButtonElement> | undefined) {
    button?.nativeElement.focus();
  }

  @ViewChild('receiveDialog')
  set receiveDialog(dialog: ElementRef<HTMLElement> | undefined) {
    dialog?.nativeElement.focus();
  }

  readonly order = signal<PurchaseOrderDetails | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly pendingCancel = signal(false);
  readonly canceling = signal(false);
  readonly cancelError = signal<string | null>(null);
  readonly cancelActionError = signal<string | null>(null);
  readonly pendingApprove = signal(false);
  readonly approving = signal(false);
  readonly approveError = signal<string | null>(null);
  readonly approveActionError = signal<string | null>(null);
  readonly pendingReceive = signal(false);
  readonly receiving = signal(false);
  readonly receiveError = signal<string | null>(null);
  readonly receiveActionError = signal<string | null>(null);
  readonly receiveLines = signal<PurchaseOrderLineDetails[]>([]);
  readonly receiveForm = new FormArray<FormControl<number | null>>([]);
  private readonly receiveFormValue = toSignal(
    this.receiveForm.valueChanges.pipe(startWith(this.receiveForm.value)),
    { requireSync: true },
  );
  readonly actionBusy = computed(() => this.canceling() || this.approving() || this.receiving());
  readonly canSubmitReceive = computed(() => {
    this.receiveFormValue();
    return (
      !this.receiving() &&
      this.receiveForm.valid &&
      this.receiveForm.controls.some((control) => (control.value ?? 0) > 0)
    );
  });
  readonly totalExpected = computed(
    () => this.order()?.lines.reduce((sum, line) => sum + line.expectedQuantity, 0) ?? 0,
  );
  readonly totalReceived = computed(
    () => this.order()?.lines.reduce((sum, line) => sum + line.receivedQuantity, 0) ?? 0,
  );
  readonly canEdit = computed(
    () =>
      this.order()?.status === 'Draft' &&
      !this.auth.hasRole('SystemAdmin') &&
      this.auth.hasAnyRole(MANAGE_INBOUND_ROLES),
  );
  readonly canReceive = computed(
    () =>
      (this.order()?.status === 'Pending' || this.order()?.status === 'Receiving') &&
      !this.auth.hasRole('SystemAdmin') &&
      this.auth.hasAnyRole(RECEIVE_INBOUND_ROLES),
  );

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
          this.pendingCancel.set(false);
          this.cancelError.set(null);
          this.cancelActionError.set(null);
          this.pendingApprove.set(false);
          this.approveError.set(null);
          this.approveActionError.set(null);
          this.pendingReceive.set(false);
          this.receiveLines.set([]);
          this.receiveForm.clear();
          this.receiveForm.enable({ emitEvent: false });
          this.receiveError.set(null);
          this.receiveActionError.set(null);
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
      });
  }

  retry(): void {
    this.reload$.next();
  }

  requestCancel(event: Event): void {
    if (!this.canEdit() || this.actionBusy() || this.pendingApprove()) return;
    this.cancelTrigger = event.currentTarget as HTMLElement;
    this.cancelError.set(null);
    this.cancelActionError.set(null);
    this.pendingCancel.set(true);
  }

  closeCancel(): void {
    if (this.canceling()) return;
    this.pendingCancel.set(false);
    this.cancelError.set(null);
    queueMicrotask(() => this.cancelTrigger?.focus());
  }

  confirmCancel(): void {
    const order = this.order();
    if (!order || !this.pendingCancel() || !this.canEdit() || this.actionBusy()) return;
    this.canceling.set(true);
    this.cancelError.set(null);
    this.ordersService
      .cancelPurchaseOrder(order.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.canceling.set(false);
          this.pendingCancel.set(false);
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.canceling.set(false);
          if (error.status === 409) {
            this.pendingCancel.set(false);
            this.reload$.next();
            this.cancelActionError.set(
              'This Purchase Order changed while canceling. Its details have been refreshed; review them before trying again.',
            );
          } else if (error.status === 404) {
            this.pendingCancel.set(false);
            this.reload$.next();
          } else {
            this.cancelError.set(
              error.status === 401 || error.status === 403
                ? 'You are not authorized to cancel this Purchase Order.'
                : 'Could not cancel this Purchase Order. Please try again.',
            );
          }
        },
      });
  }

  requestApprove(event: Event): void {
    if (!this.canEdit() || this.actionBusy() || this.pendingCancel()) return;
    this.approveTrigger = event.currentTarget as HTMLElement;
    this.approveError.set(null);
    this.approveActionError.set(null);
    this.pendingApprove.set(true);
  }

  closeApprove(): void {
    if (this.approving()) return;
    this.pendingApprove.set(false);
    this.approveError.set(null);
    queueMicrotask(() => this.approveTrigger?.focus());
  }

  confirmApprove(): void {
    const order = this.order();
    if (!order || !this.pendingApprove() || !this.canEdit() || this.actionBusy()) return;
    this.approving.set(true);
    this.approveError.set(null);
    this.ordersService
      .approvePurchaseOrder(order.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.approving.set(false);
          this.pendingApprove.set(false);
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.approving.set(false);
          if (error.status === 409) {
            this.pendingApprove.set(false);
            this.reload$.next();
            this.approveActionError.set(
              'This Purchase Order changed while approving. Its details have been refreshed; review them before trying again.',
            );
          } else if (error.status === 404) {
            this.pendingApprove.set(false);
            this.reload$.next();
          } else {
            this.approveError.set(
              error.status === 401 || error.status === 403
                ? 'You are not authorized to approve this Purchase Order.'
                : 'Could not approve this Purchase Order. Please try again.',
            );
          }
        },
      });
  }

  requestReceive(event: Event): void {
    const order = this.order();
    if (!order || !this.canReceive() || this.actionBusy()) return;
    this.receiveTrigger = event.currentTarget as HTMLElement;
    this.receiveLines.set(order.lines);
    this.receiveForm.clear();
    for (const line of order.lines) {
      this.receiveForm.push(
        new FormControl<number | null>(null, this.receiveQuantityValidator(line.remainingQuantity)),
      );
    }
    this.receiveError.set(null);
    this.receiveActionError.set(null);
    this.pendingReceive.set(true);
  }

  closeReceive(): void {
    if (this.receiving()) return;
    this.pendingReceive.set(false);
    this.receiveError.set(null);
    this.receiveForm.clear();
    this.receiveForm.enable({ emitEvent: false });
    this.receiveLines.set([]);
    queueMicrotask(() => this.receiveTrigger?.focus());
  }

  fillAllRemaining(): void {
    if (this.receiving()) return;
    this.receiveLines().forEach((line, index) => {
      if (line.remainingQuantity > 0) this.receiveForm.at(index).setValue(line.remainingQuantity);
    });
  }

  receiveQuantityError(index: number): string | null {
    this.receiveFormValue();
    const control = this.receiveForm.at(index);
    if (control.value === null || control.valid) return null;
    if (control.hasError('overRemaining')) {
      return `Cannot receive more than the remaining quantity (${this.receiveLines()[index].remainingQuantity}).`;
    }
    if (control.hasError('precision')) return 'Use at most 16 whole digits and two decimal places.';
    return 'Enter a quantity greater than zero.';
  }

  confirmReceive(): void {
    const order = this.order();
    if (!order || !this.pendingReceive() || !this.canReceive() || !this.canSubmitReceive()) return;
    const lines = this.receiveLines().flatMap((line, index) => {
      const quantity = this.receiveForm.at(index).value;
      return quantity !== null && quantity > 0
        ? [{ purchaseOrderLineId: line.id, quantity }]
        : [];
    });
    this.receiving.set(true);
    this.receiveError.set(null);
    this.receiveForm.disable({ emitEvent: false });
    this.ordersService
      .receivePurchaseOrder(order.id, { lines })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.receiving.set(false);
          this.closeReceive();
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.receiving.set(false);
          this.receiveForm.enable({ emitEvent: false });
          if (error.status === 409) {
            this.closeReceive();
            this.reload$.next();
            this.receiveActionError.set(
              'This Purchase Order changed or the receipt could not be applied. Its details have been refreshed; review them before trying again.',
            );
          } else if (error.status === 404) {
            this.closeReceive();
            this.reload$.next();
          } else {
            this.receiveError.set(
              error.status === 401 || error.status === 403
                ? 'You are not authorized to receive this Purchase Order.'
                : error.status === 400
                  ? 'Please check the receipt quantities.'
                  : 'Could not receive these items. Please try again.',
            );
          }
        },
      });
  }

  private receiveQuantityValidator(remainingQuantity: number): ValidatorFn {
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

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.pendingCancel()) this.closeCancel();
    if (this.pendingApprove()) this.closeApprove();
    if (this.pendingReceive()) this.closeReceive();
  }
}
