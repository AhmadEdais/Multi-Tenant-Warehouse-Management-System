import { PageHeaderComponent } from '../../../shared/components/page-header/page-header';
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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.services';
import {
  catchError,
  combineLatest,
  distinctUntilChanged,
  EMPTY,
  map,
  of,
  startWith,
  Subject,
  switchMap,
} from 'rxjs';
import {
  SalesOrderAllocationStatus,
  SalesOrderAvailabilityResult,
  SalesOrderDetails,
  SalesOrderPickupResult,
} from '../models/sales-order';
import { CONFIRM_OUTBOUND_PICKUP_ROLES, MANAGE_OUTBOUND_ROLES } from '../permissions';
import { SalesOrderApprovalDialogComponent } from '../sales-order-approval-dialog/sales-order-approval-dialog';
import { SalesOrderPickupDialogComponent } from '../sales-order-pickup-dialog/sales-order-pickup-dialog';
import { SalesOrderPickupHistoryComponent } from '../sales-order-pickup-history/sales-order-pickup-history';
import { SalesOrderStatusBadge } from '../sales-order-status-badge/sales-order-status-badge';
import { SalesOrdersService } from '../sales-orders.service';

@Component({
  selector: 'app-sales-order-details-page',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    SalesOrderApprovalDialogComponent,
    SalesOrderPickupDialogComponent,
    SalesOrderPickupHistoryComponent,
    SalesOrderStatusBadge,
    PageHeaderComponent,
  ],
  templateUrl: './sales-order-details-page.html',
})
export class SalesOrderDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ordersService = inject(SalesOrdersService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private readonly availabilityCheck$ = new Subject<SalesOrderDetails | null>();
  private approvalTrigger: HTMLElement | null = null;
  private cancelTrigger: HTMLElement | null = null;
  private pickupTrigger: HTMLElement | null = null;
  private activeRouteId: string | null = null;
  private refreshApprovalAfterReload = false;

  @ViewChild('salesOrdersBack') private backLink?: ElementRef<HTMLAnchorElement>;
  @ViewChild('cancelDialog') private cancelDialog?: ElementRef<HTMLElement>;

  @ViewChild('keepOrderButton')
  set keepOrderButton(button: ElementRef<HTMLButtonElement> | undefined) {
    button?.nativeElement.focus();
  }

  readonly order = signal<SalesOrderDetails | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly created = signal(false);
  readonly approved = signal(false);
  readonly canceled = signal(false);
  readonly pendingCancel = signal(false);
  readonly canceling = signal(false);
  readonly cancelError = signal<string | null>(null);
  readonly cancelActionError = signal<string | null>(null);
  readonly approvalOrder = signal<SalesOrderDetails | null>(null);
  readonly approvalAvailability = signal<SalesOrderAvailabilityResult | null>(null);
  readonly checkingApprovalAvailability = signal(false);
  readonly approvalAvailabilityError = signal<string | null>(null);
  readonly approvalError = signal<string | null>(null);
  readonly approvalNotice = signal<string | null>(null);
  readonly approvalActionError = signal<string | null>(null);
  readonly approving = signal(false);
  readonly pickupOpen = signal(false);
  readonly pickupBusy = signal(false);
  readonly pickupSuccess = signal<string | null>(null);
  readonly pickupActionError = signal<string | null>(null);
  readonly allocationCount = computed(
    () => this.order()?.lines.reduce((count, line) => count + line.allocations.length, 0) ?? 0,
  );
  readonly canEdit = computed(
    () =>
      this.order()?.status === 'Draft' &&
      !this.auth.hasRole('SystemAdmin') &&
      this.auth.hasAnyRole(MANAGE_OUTBOUND_ROLES),
  );
  readonly canCancel = computed(
    () =>
      (this.order()?.status === 'Draft' || this.order()?.status === 'Allocated') &&
      !this.auth.hasRole('SystemAdmin') &&
      this.auth.hasAnyRole(MANAGE_OUTBOUND_ROLES),
  );
  readonly canPickup = computed(
    () =>
      (this.order()?.status === 'Allocated' || this.order()?.status === 'Picked') &&
      !this.auth.hasRole('SystemAdmin') &&
      this.auth.hasAnyRole(CONFIRM_OUTBOUND_PICKUP_ROLES),
  );
  readonly actionBusy = computed(() => this.approving() || this.canceling() || this.pickupBusy());
  readonly canConfirmApproval = computed(() => {
    const order = this.order();
    const result = this.approvalAvailability();
    return (
      this.canEdit() &&
      !this.loading() &&
      !this.actionBusy() &&
      !this.pendingCancel() &&
      !this.checkingApprovalAvailability() &&
      !this.approvalAvailabilityError() &&
      !this.approvalError() &&
      !!order &&
      !!result &&
      result.warehouseId === order.warehouseId &&
      result.allLinesSufficient &&
      result.lines.length === order.lines.length &&
      order.lines.every((line) =>
        result.lines.some(
          (stock) =>
            stock.productId === line.productId &&
            stock.requestedQuantity === line.quantity &&
            stock.hasSufficientStock,
        ),
      )
    );
  });
  readonly allocationStatusClasses: Record<SalesOrderAllocationStatus, string> = {
    Active: 'bg-[#e7efff] text-[#2458c5]',
    Released: 'bg-[#eef2f7] text-[#52647e]',
    Shipped: 'bg-[#e1f7eb] text-[#116b48]',
  };

  constructor() {
    if (history.state?.['salesOrderCreated'] === true) {
      this.created.set(true);
      history.replaceState({ ...history.state, salesOrderCreated: false }, '');
    }
  }

  ngOnInit(): void {
    this.availabilityCheck$
      .pipe(
        switchMap((order) => {
          if (!order) return EMPTY;
          return this.ordersService
            .checkAvailability({
              warehouseId: order.warehouseId,
              lines: order.lines.map((line) => ({
                productId: line.productId,
                quantity: line.quantity,
              })),
            })
            .pipe(
              map((result) => ({ orderId: order.id, result })),
              catchError((error: HttpErrorResponse) => {
                if (this.approvalOrder()?.id === order.id) {
                  this.checkingApprovalAvailability.set(false);
                  this.approvalAvailabilityError.set(
                    error.status === 401 || error.status === 403
                      ? 'You are not authorized to check stock availability.'
                      : error.status === 404
                        ? 'The warehouse or a product is no longer available. Refresh the order before approving.'
                        : 'Could not check available stock. Try again before approving.',
                  );
                }
                return EMPTY;
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ orderId, result }) => {
        if (this.approvalOrder()?.id !== orderId) return;
        this.approvalAvailability.set(result);
        this.checkingApprovalAvailability.set(false);
      });

    combineLatest([
      this.route.paramMap.pipe(
        map((params) => params.get('id')),
        distinctUntilChanged(),
      ),
      this.reload$.pipe(startWith(undefined)),
    ])
      .pipe(
        switchMap(([rawId]) => {
          if (rawId !== this.activeRouteId) {
            this.activeRouteId = rawId;
            this.resetApproval();
            this.approved.set(false);
            this.approvalActionError.set(null);
            this.canceled.set(false);
            this.cancelActionError.set(null);
            this.pickupSuccess.set(null);
            this.pickupActionError.set(null);
          }
          this.pickupOpen.set(false);
          this.pickupBusy.set(false);
          this.pendingCancel.set(false);
          this.cancelError.set(null);
          this.loading.set(true);
          this.order.set(null);
          this.notFound.set(false);
          this.error.set(null);

          if (
            !rawId ||
            !/^\d+$/.test(rawId) ||
            !Number.isSafeInteger(Number(rawId)) ||
            Number(rawId) <= 0
          ) {
            this.notFound.set(true);
            return of(null);
          }

          return this.ordersService.getSalesOrderById(Number(rawId)).pipe(
            catchError((error: HttpErrorResponse) => {
              if (error.status === 404) this.notFound.set(true);
              else if (error.status === 401 || error.status === 403)
                this.error.set('You are not authorized to view this Sales Order.');
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
        if (this.refreshApprovalAfterReload) {
          this.refreshApprovalAfterReload = false;
          if (order?.status === 'Draft') {
            this.approvalOrder.set(order);
            this.checkApprovalAvailability(order);
          } else {
            this.resetApproval();
            this.approvalActionError.set(
              'This Sales Order changed and can no longer be approved. Its details have been refreshed.',
            );
          }
        }
      });
  }

  retry(): void {
    this.reload$.next();
  }

  openApproval(event: Event): void {
    const order = this.order();
    if (
      !order ||
      !this.canEdit() ||
      this.actionBusy() ||
      this.approvalOrder() ||
      this.pendingCancel() ||
      this.pickupOpen()
    )
      return;
    this.approvalTrigger = event.currentTarget as HTMLElement;
    this.approvalError.set(null);
    this.approvalNotice.set(null);
    this.approvalActionError.set(null);
    this.approvalOrder.set(order);
    this.checkApprovalAvailability(order);
  }

  retryApprovalAvailability(): void {
    const order = this.approvalOrder();
    if (order && !this.approving()) {
      this.approvalError.set(null);
      this.checkApprovalAvailability(order);
    }
  }

  closeApproval(): void {
    if (this.approving()) return;
    this.resetApproval();
    queueMicrotask(() => {
      if (this.approvalTrigger?.isConnected) this.approvalTrigger.focus();
      else this.backLink?.nativeElement.focus();
    });
  }

  editFromApproval(): void {
    const id = this.approvalOrder()?.id;
    if (!id || this.approving()) return;
    this.closeApproval();
    void this.router.navigate(['/sales-orders', id, 'edit']);
  }

  confirmApproval(): void {
    const order = this.order();
    if (
      !order ||
      order.id !== this.approvalOrder()?.id ||
      !this.canConfirmApproval() ||
      this.pendingCancel()
    )
      return;
    this.approving.set(true);
    this.approvalError.set(null);
    this.ordersService
      .approveSalesOrder(order.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.approving.set(false);
          if (Number(this.activeRouteId) !== order.id) return;
          this.closeApproval();
          this.approved.set(true);
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.approving.set(false);
          if (Number(this.activeRouteId) !== order.id) return;
          if (error.status === 409) {
            const detail = typeof error.error?.detail === 'string' ? error.error.detail : null;
            this.approvalNotice.set(
              detail ||
                'Stock or order status changed. Review the refreshed availability before trying again.',
            );
            this.approvalAvailability.set(null);
            this.checkingApprovalAvailability.set(true);
            this.refreshApprovalAfterReload = true;
            this.reload$.next();
          } else if (error.status === 404) {
            this.closeApproval();
            this.reload$.next();
          } else {
            this.approvalError.set(
              error.status === 401 || error.status === 403
                ? 'You are not authorized to approve this Sales Order.'
                : error.status === 400
                  ? 'This Sales Order cannot be approved. Review its details and try again.'
                  : 'Could not approve this Sales Order. Please try again.',
            );
          }
        },
      });
  }

  requestCancel(event: Event): void {
    if (!this.canCancel() || this.actionBusy() || this.pendingCancel() || this.approvalOrder() || this.pickupOpen())
      return;
    this.cancelTrigger = event.currentTarget as HTMLElement;
    this.cancelError.set(null);
    this.cancelActionError.set(null);
    this.pendingCancel.set(true);
  }

  closeCancel(): void {
    if (this.canceling()) return;
    this.pendingCancel.set(false);
    this.cancelError.set(null);
    queueMicrotask(() => {
      if (this.cancelTrigger?.isConnected) this.cancelTrigger.focus();
      else this.backLink?.nativeElement.focus();
    });
  }

  trapCancelTab(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    const buttons = Array.from(
      this.cancelDialog?.nativeElement.querySelectorAll<HTMLButtonElement>(
        'button:not([disabled])',
      ) ?? [],
    );
    if (!buttons.length) {
      keyboardEvent.preventDefault();
      this.cancelDialog?.nativeElement.focus();
      return;
    }
    const first = buttons[0];
    const last = buttons[buttons.length - 1];
    if (
      keyboardEvent.shiftKey &&
      (document.activeElement === first ||
        document.activeElement === this.cancelDialog?.nativeElement)
    ) {
      keyboardEvent.preventDefault();
      last.focus();
    } else if (!keyboardEvent.shiftKey && document.activeElement === last) {
      keyboardEvent.preventDefault();
      first.focus();
    }
  }

  confirmCancel(): void {
    const order = this.order();
    if (
      !order ||
      !this.pendingCancel() ||
      !this.canCancel() ||
      this.actionBusy() ||
      this.approvalOrder() ||
      this.pickupOpen()
    )
      return;
    this.canceling.set(true);
    this.cancelError.set(null);
    queueMicrotask(() => this.cancelDialog?.nativeElement.focus());
    this.ordersService
      .cancelSalesOrder(order.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.canceling.set(false);
          if (Number(this.activeRouteId) !== order.id) return;
          this.closeCancel();
          this.canceled.set(true);
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.canceling.set(false);
          if (Number(this.activeRouteId) !== order.id) return;
          if (error.status === 409) {
            this.closeCancel();
            this.reload$.next();
            this.cancelActionError.set(
              'This Sales Order or its reservations changed. Review the latest details before confirming cancellation again.',
            );
          } else if (error.status === 404) {
            this.closeCancel();
            this.reload$.next();
          } else {
            this.cancelError.set(
              error.status === 400
                ? 'This Sales Order cannot be canceled. Review its details and try again.'
                : error.status === 401 || error.status === 403
                  ? 'You are not authorized to cancel this Sales Order.'
                  : 'Could not cancel this Sales Order. Please try again.',
            );
          }
        },
      });
  }

  openPickup(event: Event): void {
    if (!this.canPickup() || this.actionBusy() || this.pendingCancel() || this.approvalOrder() || this.pickupOpen())
      return;
    this.pickupTrigger = event.currentTarget as HTMLElement;
    this.pickupActionError.set(null);
    this.pickupOpen.set(true);
  }

  closePickup(): void {
    if (this.pickupBusy()) return;
    this.pickupOpen.set(false);
    queueMicrotask(() => {
      if (this.pickupTrigger?.isConnected) this.pickupTrigger.focus();
      else this.backLink?.nativeElement.focus();
    });
  }

  onPickupCompleted(result: SalesOrderPickupResult): void {
    if (Number(this.activeRouteId) !== result.salesOrderId) return;
    this.closePickup();
    this.pickupSuccess.set(`Pickup confirmed: ${result.totalPickedQuantity} units collected.`);
    this.reload$.next();
  }

  onPickupConflict(): void {
    this.closePickup();
    this.pickupActionError.set(
      'This Sales Order or its allocations changed. Review the refreshed quantities before confirming another pickup.',
    );
    this.reload$.next();
  }

  onPickupUnavailable(): void {
    this.closePickup();
    this.pickupActionError.set('The Sales Order or an allocation is no longer available. Review the refreshed details.');
    this.reload$.next();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.pendingCancel()) this.closeCancel();
  }

  private checkApprovalAvailability(order: SalesOrderDetails): void {
    this.approvalAvailability.set(null);
    this.approvalAvailabilityError.set(null);
    this.checkingApprovalAvailability.set(true);
    this.availabilityCheck$.next(order);
  }

  private resetApproval(): void {
    this.availabilityCheck$.next(null);
    this.refreshApprovalAfterReload = false;
    this.approvalOrder.set(null);
    this.approvalAvailability.set(null);
    this.checkingApprovalAvailability.set(false);
    this.approvalAvailabilityError.set(null);
    this.approvalError.set(null);
    this.approvalNotice.set(null);
  }
}
