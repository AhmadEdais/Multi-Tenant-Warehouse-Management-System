import { DecimalPipe } from '@angular/common';
import {
  AfterViewInit,
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
  viewChild,
} from '@angular/core';
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
import { ProductListItem } from '../../products/models/product';
import { ProductsService } from '../../products/products.service';

@Component({
  selector: 'app-product-picker',
  imports: [DecimalPipe],
  templateUrl: './product-picker.html',
})
export class ProductPickerComponent implements OnInit, AfterViewInit {
  private readonly productsService = inject(ProductsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();
  private readonly searchField = viewChild<ElementRef<HTMLInputElement>>('searchField');

  readonly currentProductId = input<number | null>(null);
  readonly unavailableProductIds = input<readonly number[]>([]);
  readonly selected = output<ProductListItem>();
  readonly close = output<void>();

  readonly pageSize = 20;
  readonly page = signal(1);
  readonly search = signal('');
  readonly appliedSearch = signal('');
  readonly products = signal<ProductListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchPending = computed(() => this.search().trim() !== this.appliedSearch());
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly firstItem = computed(() =>
    this.totalCount() ? (this.page() - 1) * this.pageSize + 1 : 0,
  );
  readonly lastItem = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));

  ngOnInit(): void {
    merge(
      this.searchInput$.pipe(
        debounceTime(300),
        map((value) => value.trim()),
        distinctUntilChanged(),
        tap((value) => {
          this.appliedSearch.set(value);
          this.page.set(1);
        }),
      ),
      this.reload$,
    )
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.productsService
            .list({
              searchTerm: this.appliedSearch(),
              categoryId: null,
              isActive: true,
              pageNumber: this.page(),
              pageSize: this.pageSize,
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load products. Please try again.');
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

  ngAfterViewInit(): void {
    this.searchField()?.nativeElement.focus();
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchInput$.next(value);
  }

  goToPage(page: number): void {
    if (
      this.loading() ||
      this.searchPending() ||
      page < 1 ||
      page > this.pageCount() ||
      page === this.page()
    )
      return;
    this.page.set(page);
    this.reload$.next();
  }

  retry(): void {
    this.reload$.next();
  }

  isUnavailable(product: ProductListItem): boolean {
    return (
      !product.isActive ||
      product.id === this.currentProductId() ||
      this.unavailableProductIds().includes(product.id)
    );
  }

  choose(product: ProductListItem): void {
    if (!this.loading() && !this.searchPending() && !this.isUnavailable(product))
      this.selected.emit(product);
  }

  dismiss(): void {
    this.close.emit();
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event): void {
    event.stopImmediatePropagation();
    this.dismiss();
  }
}
