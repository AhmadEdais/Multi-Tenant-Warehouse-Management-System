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
import { PurchaseOrderDetails } from '../models/purchase-order';
import { MANAGE_INBOUND_ROLES } from '../permissions';
import { PurchaseOrderStatusBadge } from '../purchase-order-status-badge/purchase-order-status-badge';
import { PurchaseOrdersService } from '../purchase-orders.service';

@Component({
  selector: 'app-purchase-order-details-page',
  imports: [DatePipe, DecimalPipe, RouterLink, PurchaseOrderStatusBadge],
  templateUrl: './purchase-order-details-page.html',
})
export class PurchaseOrderDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ordersService = inject(PurchaseOrdersService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private cancelTrigger: HTMLElement | null = null;

  @ViewChild('keepDraftButton')
  set keepDraftButton(button: ElementRef<HTMLButtonElement> | undefined) {
    button?.nativeElement.focus();
  }

  readonly order = signal<PurchaseOrderDetails | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly pendingCancel = signal(false);
  readonly canceling = signal(false);
  readonly cancelError = signal<string | null>(null);
  readonly cancelActionError = signal<string | null>(null);
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
    if (!this.canEdit() || this.canceling()) return;
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
    if (!order || !this.pendingCancel() || !this.canEdit() || this.canceling()) return;
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

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.pendingCancel()) this.closeCancel();
  }
}
