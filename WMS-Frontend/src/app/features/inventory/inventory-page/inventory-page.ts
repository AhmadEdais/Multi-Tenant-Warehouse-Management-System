import { DecimalPipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  merge,
  of,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { InventoryService } from '../inventory.service';
import { TenantStockSummary } from '../models/inventory';
import { ProductInventoryDetailsComponent } from '../product-inventory-details/product-inventory-details';

@Component({
  selector: 'app-inventory-page',
  imports: [DecimalPipe, ProductInventoryDetailsComponent],
  templateUrl: './inventory-page.html',
})
export class InventoryPage implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  readonly pageSize = 10;
  readonly page = signal(1);
  readonly search = signal('');
  readonly appliedSearch = signal('');
  readonly products = signal<TenantStockSummary[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedProduct = signal<TenantStockSummary | null>(null);

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly firstItem = computed(() =>
    this.totalCount() ? (this.page() - 1) * this.pageSize + 1 : 0,
  );
  readonly lastItem = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));
  readonly pageLowStock = computed(
    () => this.products().filter((product) => product.isLowStock).length,
  );
  readonly pageHealthy = computed(() => this.products().length - this.pageLowStock());

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
    )
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.inventoryService
            .getSummary({
              searchTerm: this.appliedSearch(),
              pageNumber: this.page(),
              pageSize: this.pageSize,
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load inventory. Please try again.');
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        this.products.set(result?.data ?? []);
        this.totalCount.set(result?.totalCount ?? 0);
        if (result && this.page() > this.pageCount()) {
          this.page.set(this.pageCount());
          this.reload$.next();
        }
      });

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

  openProduct(product: TenantStockSummary): void {
    this.selectedProduct.set(product);
  }

  closeProduct(): void {
    this.selectedProduct.set(null);
  }
}
