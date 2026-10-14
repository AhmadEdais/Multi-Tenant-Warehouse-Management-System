import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, combineLatest, of, startWith, Subject, switchMap } from 'rxjs';
import { SalesOrderPickup } from '../models/sales-order-pickup';
import { SalesOrdersService } from '../sales-orders.service';

@Component({
  selector: 'app-sales-order-pickup-history',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './sales-order-pickup-history.html',
})
export class SalesOrderPickupHistoryComponent implements OnInit {
  private readonly ordersService = inject(SalesOrdersService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly retry$ = new Subject<void>();

  readonly orderId = input.required<number>();
  private readonly orderId$ = toObservable(this.orderId);
  readonly pickups = signal<SalesOrderPickup[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly expandedId = signal<number | null>(null);
  readonly totalPicked = computed(() =>
    this.pickups().reduce((sum, pickup) => sum + pickup.totalPickedQuantity, 0),
  );

  ngOnInit(): void {
    combineLatest([this.orderId$, this.retry$.pipe(startWith(undefined))])
      .pipe(
        switchMap(([id]) => {
          this.loading.set(true);
          this.error.set(null);
          this.pickups.set([]);
          this.expandedId.set(null);
          return this.ordersService.getSalesOrderPickups(id).pipe(
            catchError((error: HttpErrorResponse) => {
              this.error.set(
                error.status === 401 || error.status === 403
                  ? 'You are not authorized to view pickup history.'
                  : error.status === 404
                    ? 'This Sales Order is no longer available.'
                    : 'Could not load pickup history. Please try again.',
              );
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((pickups) => {
        this.loading.set(false);
        if (pickups) this.pickups.set(pickups);
      });
  }

  retry(): void {
    this.retry$.next();
  }

  toggle(pickupId: number): void {
    this.expandedId.update((current) => (current === pickupId ? null : pickupId));
  }
}
