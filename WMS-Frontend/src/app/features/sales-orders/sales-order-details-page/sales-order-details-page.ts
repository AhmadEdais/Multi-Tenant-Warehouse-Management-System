import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, combineLatest, distinctUntilChanged, map, of, startWith, Subject, switchMap } from 'rxjs';
import { SalesOrderAllocationStatus, SalesOrderDetails } from '../models/sales-order';
import { SalesOrderStatusBadge } from '../sales-order-status-badge/sales-order-status-badge';
import { SalesOrdersService } from '../sales-orders.service';

@Component({
  selector: 'app-sales-order-details-page',
  imports: [DatePipe, DecimalPipe, RouterLink, SalesOrderStatusBadge],
  templateUrl: './sales-order-details-page.html',
})
export class SalesOrderDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ordersService = inject(SalesOrdersService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();

  readonly order = signal<SalesOrderDetails | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly created = signal(false);
  readonly allocationCount = computed(() =>
    this.order()?.lines.reduce((count, line) => count + line.allocations.length, 0) ?? 0,
  );
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
    combineLatest([
      this.route.paramMap.pipe(
        map((params) => params.get('id')),
        distinctUntilChanged(),
      ),
      this.reload$.pipe(startWith(undefined)),
    ])
      .pipe(
        switchMap(([rawId]) => {
          this.loading.set(true);
          this.order.set(null);
          this.notFound.set(false);
          this.error.set(null);

          if (!rawId || !/^\d+$/.test(rawId) || !Number.isSafeInteger(Number(rawId)) || Number(rawId) <= 0) {
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
      });
  }

  retry(): void {
    this.reload$.next();
  }
}
