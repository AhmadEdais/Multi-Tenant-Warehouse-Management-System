import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, EMPTY, expand, map, merge, of, reduce, Subject, switchMap, tap } from 'rxjs';
import { Warehouse } from '../../warehouses/models/warehouse';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { PutawayQueueItem, PutawayRequest } from '../models/putaway';
import { PutawayDialogComponent } from '../putaway-dialog/putaway-dialog';
import { PutawayService } from '../putaway.service';

@Component({
  selector: 'app-putaway-page',
  imports: [DecimalPipe, PutawayDialogComponent],
  templateUrl: './putaway-page.html',
})
export class PutawayPage implements OnInit {
  private readonly warehousesService = inject(WarehousesService);
  private readonly putawayService = inject(PutawayService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();
  private modalTrigger: HTMLElement | null = null;

  readonly pageSize = 20;
  readonly warehouses = signal<Warehouse[]>([]);
  readonly warehousesLoading = signal(true);
  readonly warehousesError = signal<string | null>(null);
  readonly warehouseId = signal<number | null>(null);
  readonly warehouseName = computed(() => this.warehouses().find((warehouse) => warehouse.id === this.warehouseId())?.name ?? '');
  readonly search = signal('');
  readonly appliedSearch = signal('');
  readonly page = signal(1);
  readonly items = signal<PutawayQueueItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedItem = signal<PutawayQueueItem | null>(null);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly firstItem = computed(() => this.totalCount() ? (this.page() - 1) * this.pageSize + 1 : 0);
  readonly lastItem = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));

  ngOnInit(): void {
    merge(
      this.searchInput$.pipe(
        debounceTime(300),
        map((value) => value.trim()),
        distinctUntilChanged(),
        tap((term) => {
          this.appliedSearch.set(term);
          this.page.set(1);
        }),
      ),
      this.reload$,
    ).pipe(
      switchMap(() => {
        const warehouseId = this.warehouseId();
        if (warehouseId === null) return of(null);
        this.loading.set(true);
        this.error.set(null);
        return this.putawayService.getQueue({
          warehouseId,
          searchTerm: this.appliedSearch(),
          pageNumber: this.page(),
          pageSize: this.pageSize,
        }).pipe(catchError((error: HttpErrorResponse) => {
          this.error.set(error.status === 409
            ? 'This warehouse’s Receiving Dock needs attention before putaway can continue.'
            : 'Could not load the Putaway queue. Please try again.');
          return of(null);
        }));
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((result) => {
      this.loading.set(false);
      this.items.set(result?.data ?? []);
      this.totalCount.set(result?.totalCount ?? 0);
      if (result && this.page() > this.pageCount()) {
        this.page.set(this.pageCount());
        this.reload$.next();
      }
    });
    this.loadWarehouses();
  }

  loadWarehouses(): void {
    this.warehousesLoading.set(true);
    this.warehousesError.set(null);
    this.warehousesService.list({ pageNumber: 1, pageSize: 100, search: '', isActive: true }).pipe(
      expand((result) => result.pageNumber * result.pageSize < result.totalCount
        ? this.warehousesService.list({ pageNumber: result.pageNumber + 1, pageSize: 100, search: '', isActive: true })
        : EMPTY),
      reduce((all, result) => [...all, ...result.data], [] as Warehouse[]),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (warehouses) => {
        this.warehouses.set(warehouses);
        this.warehousesLoading.set(false);
        const current = this.warehouseId();
        if (current === null || !warehouses.some((warehouse) => warehouse.id === current)) {
          this.warehouseId.set(warehouses[0]?.id ?? null);
        }
        if (this.warehouseId() !== null) this.reload$.next();
      },
      error: () => {
        this.warehousesLoading.set(false);
        this.warehousesError.set('Could not load active warehouses. Please try again.');
      },
    });
  }

  selectWarehouse(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value);
    if (!this.warehouses().some((warehouse) => warehouse.id === id) || this.submitting()) return;
    this.warehouseId.set(id);
    this.page.set(1);
    this.actionMessage.set(null);
    this.successMessage.set(null);
    this.reload$.next();
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchInput$.next(value);
  }

  goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.pageCount() || page === this.page()) return;
    this.page.set(page);
    this.reload$.next();
  }

  retry(): void {
    this.reload$.next();
  }

  openPutaway(item: PutawayQueueItem, event: Event): void {
    if (this.loading() || this.submitting()) return;
    this.modalTrigger = event.currentTarget as HTMLElement;
    this.submitError.set(null);
    this.actionMessage.set(null);
    this.successMessage.set(null);
    this.selectedItem.set(item);
  }

  closePutaway(): void {
    if (this.submitting()) return;
    this.selectedItem.set(null);
    this.submitError.set(null);
    queueMicrotask(() => this.modalTrigger?.focus());
  }

  confirmPutaway(request: PutawayRequest): void {
    const selected = this.selectedItem();
    if (this.submitting() || !selected || request.productId !== selected.productId || request.warehouseId !== this.warehouseId()) return;
    this.submitting.set(true);
    this.submitError.set(null);
    this.putawayService.putAway(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closePutaway();
        this.successMessage.set(`${selected.sku} stock was put away.`);
        this.reload$.next();
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        if (error.status === 409 || error.status === 404) {
          this.closePutaway();
          this.actionMessage.set(error.status === 409
            ? 'Stock or destination locations changed. The queue has been refreshed; review it before trying again.'
            : 'A warehouse, product, or location is no longer available. The queue has been refreshed.');
          this.reload$.next();
        } else {
          this.submitError.set(error.status === 401 || error.status === 403
            ? 'You are not authorized to put away stock.'
            : error.status === 400
              ? 'Please check the destination Bins and quantities.'
              : 'Could not put away this stock. Please try again.');
        }
      },
    });
  }
}
