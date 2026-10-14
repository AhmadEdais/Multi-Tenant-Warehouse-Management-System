import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  ElementRef,
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
} from '../models/sales-order';
import { MANAGE_OUTBOUND_ROLES } from '../permissions';
import { SalesOrderApprovalDialogComponent } from '../sales-order-approval-dialog/sales-order-approval-dialog';
import { SalesOrderStatusBadge } from '../sales-order-status-badge/sales-order-status-badge';
import { SalesOrdersService } from '../sales-orders.service';

@Component({
  selector: 'app-sales-order-details-page',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    SalesOrderApprovalDialogComponent,
    SalesOrderStatusBadge,
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
  private activeRouteId: string | null = null;
  private refreshApprovalAfterReload = false;

  @ViewChild('salesOrdersBack') private backLink?: ElementRef<HTMLAnchorElement>;

  readonly order = signal<SalesOrderDetails | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly created = signal(false);
  readonly approved = signal(false);
  readonly approvalOrder = signal<SalesOrderDetails | null>(null);
  readonly approvalAvailability = signal<SalesOrderAvailabilityResult | null>(null);
  readonly checkingApprovalAvailability = signal(false);
  readonly approvalAvailabilityError = signal<string | null>(null);
  readonly approvalError = signal<string | null>(null);
  readonly approvalNotice = signal<string | null>(null);
  readonly approvalActionError = signal<string | null>(null);
  readonly approving = signal(false);
  readonly allocationCount = computed(
    () => this.order()?.lines.reduce((count, line) => count + line.allocations.length, 0) ?? 0,
  );
  readonly canEdit = computed(
    () =>
      this.order()?.status === 'Draft' &&
      !this.auth.hasRole('SystemAdmin') &&
      this.auth.hasAnyRole(MANAGE_OUTBOUND_ROLES),
  );
  readonly canConfirmApproval = computed(() => {
    const order = this.order();
    const result = this.approvalAvailability();
    return (
      this.canEdit() &&
      !this.loading() &&
      !this.approving() &&
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
          }
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
    if (!order || !this.canEdit() || this.approving() || this.approvalOrder()) return;
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
    if (!order || order.id !== this.approvalOrder()?.id || !this.canConfirmApproval()) return;
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
