import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  effect,
  HostListener,
  inject,
  OnInit,
  signal,
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
import { AuthService } from '../../auth/auth.services';
import { CategoriesService } from '../../categories/categories.service';
import { CategoryParentMenuItem } from '../../categories/category-parent-menu-item/category-parent-menu-item';
import { CategoryNode } from '../../categories/models/category';
import {
  CreateProductRequest,
  ProductDetails,
  ProductFormValue,
  ProductListItem,
  UpdateProductRequest,
} from '../models/product';
import { ProductFormComponent } from '../product-form/product-form';
import { ProductsService } from '../products.service';

type ProductStatus = 'all' | 'active' | 'inactive';
type ProductAction = 'deactivate' | 'reactivate';
type FormState = { mode: 'create' | 'edit'; product: ProductDetails | null };

@Component({
  selector: 'app-products-page',
  imports: [DecimalPipe, ProductFormComponent, CategoryParentMenuItem],
  templateUrl: './products-page.html',
  styleUrl: './products-page.css',
})
export class ProductsPage implements OnInit {
  private readonly productsService = inject(ProductsService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();
  private readonly detailsSelection$ = new Subject<number | null>();

  readonly canManage = computed(() =>
    this.authService.hasAnyRole(['TenantAdmin', 'WarehouseManager']),
  );
  readonly pageSize = 5;
  readonly products = signal<ProductListItem[]>([]);
  readonly page = signal(1);
  readonly search = signal('');
  readonly categoryId = signal<number | null>(null);
  readonly categoryPickerOpen = signal(false);
  readonly categoryPickerOpenPath = signal<number[]>([]);
  readonly categoryPickerPosition = signal({ top: 0, left: 0 });
  readonly categoryPickerOpenLeft = signal(false);
  readonly status = signal<ProductStatus>('all');
  private readonly normalizeStatusForRole = effect(() => {
    if (!this.canManage() && this.status() !== 'all') this.status.set('all');
  });
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly categoryTree = signal<CategoryNode[]>([]);
  readonly categoriesLoading = signal(false);
  readonly categoriesError = signal<string | null>(null);
  readonly selectedProductId = signal<number | null>(null);
  readonly selectedProductDetails = signal<ProductDetails | null>(null);
  readonly detailsOpen = signal(false);
  readonly detailsLoading = signal(false);
  readonly detailsError = signal<string | null>(null);
  readonly menuProduct = signal<ProductListItem | null>(null);
  readonly menuPosition = signal({ top: 0, left: 0 });
  readonly formState = signal<FormState | null>(null);
  readonly formOpening = signal(false);
  readonly formLoading = signal(false);
  readonly formError = signal<string | null>(null);
  readonly pendingCreatedId = signal<number | null>(null);
  readonly pendingAction = signal<{ action: ProductAction; product: ProductListItem } | null>(null);
  readonly actionLoading = signal(false);
  readonly actionError = signal<string | null>(null);

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly firstItem = computed(() =>
    this.totalCount() ? (this.page() - 1) * this.pageSize + 1 : 0,
  );
  readonly lastItem = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));
  readonly activeCategoryTree = computed(() => {
    const keepActive = (nodes: CategoryNode[]): CategoryNode[] =>
      nodes
        .filter((node) => node.isActive)
        .map((node) => ({ ...node, children: keepActive(node.children) }));
    return keepActive(this.categoryTree());
  });
  readonly selectedCategoryName = computed(() => {
    const id = this.categoryId();
    if (id === null) return 'All categories';
    const find = (nodes: CategoryNode[]): string | null => {
      for (const node of nodes) {
        if (node.id === id) return node.name;
        const child = find(node.children);
        if (child) return child;
      }
      return null;
    };
    return find(this.activeCategoryTree()) ?? 'All categories';
  });

  ngOnInit(): void {
    merge(
      this.searchInput$.pipe(
        debounceTime(300),
        map((value) => value.trim()),
        distinctUntilChanged(),
        tap(() => this.page.set(1)),
      ),
      this.reload$,
    )
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.productsService
            .list({
              searchTerm: this.search().trim(),
              categoryId: this.categoryId(),
              isActive:
                this.canManage() && this.status() !== 'all' ? this.status() === 'active' : null,
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

    this.detailsSelection$
      .pipe(
        switchMap((id) => {
          this.selectedProductId.set(id);
          this.selectedProductDetails.set(null);
          this.detailsError.set(null);
          this.detailsLoading.set(id !== null);
          if (id === null) return of(null);
          return this.productsService.getById(id).pipe(
            catchError(() => {
              this.detailsError.set('Could not load product details. Please try again.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((details) => {
        this.detailsLoading.set(false);
        this.selectedProductDetails.set(details);
      });

    this.loadCategories();
    this.reload$.next();
  }

  loadCategories(): void {
    this.categoriesLoading.set(true);
    this.categoriesError.set(null);
    this.categoriesService
      .getTree()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tree) => {
          this.categoryTree.set(tree);
          this.categoriesLoading.set(false);
        },
        error: () => {
          this.categoriesLoading.set(false);
          this.categoriesError.set('Could not load categories. Please try again.');
        },
      });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchInput$.next(value);
  }

  toggleCategoryPicker(event: MouseEvent): void {
    event.stopPropagation();
    if (this.categoriesLoading() || this.categoriesError()) return;
    if (this.categoryPickerOpen()) {
      this.categoryPickerOpen.set(false);
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const left = Math.max(8, Math.min(rect.left, window.innerWidth - 232));
    this.categoryPickerPosition.set({
      top: Math.max(8, Math.min(rect.bottom + 4, window.innerHeight - 270)),
      left,
    });
    this.categoryPickerOpenLeft.set(left + 456 > window.innerWidth);
    this.categoryPickerOpenPath.set([]);
    this.categoryPickerOpen.set(true);
  }

  chooseCategory(category: CategoryNode | null): void {
    this.categoryPickerOpen.set(false);
    const id = category?.id ?? null;
    if (id === this.categoryId()) return;
    this.categoryId.set(id);
    this.page.set(1);
    this.reload$.next();
  }

  onStatusChange(event: Event): void {
    if (!this.canManage()) return;
    this.status.set((event.target as HTMLSelectElement).value as ProductStatus);
    this.page.set(1);
    this.reload$.next();
  }

  goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.pageCount() || page === this.page()) return;
    this.page.set(page);
    this.reload$.next();
  }

  retry(): void {
    this.reload$.next();
  }

  openDetails(product: ProductListItem): void {
    this.closeMenu();
    this.detailsOpen.set(true);
    this.detailsSelection$.next(product.id);
  }

  retryDetails(): void {
    const id = this.selectedProductId();
    if (id !== null) this.detailsSelection$.next(id);
  }

  closeDetails(): void {
    this.detailsOpen.set(false);
    this.detailsSelection$.next(null);
  }

  openMenu(product: ProductListItem, event: MouseEvent): void {
    event.stopPropagation();
    if (!this.canManage()) return;
    if (this.menuProduct()?.id === product.id) {
      this.closeMenu();
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const width = 176;
    this.menuPosition.set({
      top: Math.max(8, Math.min(rect.bottom + 5, window.innerHeight - 100)),
      left: Math.max(8, Math.min(rect.right - width, window.innerWidth - width - 8)),
    });
    this.menuProduct.set(product);
  }

  closeMenu(): void {
    this.menuProduct.set(null);
  }

  openCreateForm(): void {
    if (!this.canManage()) return;
    this.closeMenu();
    this.formError.set(null);
    this.pendingCreatedId.set(null);
    this.formState.set({ mode: 'create', product: null });
  }

  openEditForm(product: ProductListItem | ProductDetails): void {
    if (!this.canManage() || !product.isActive || this.formOpening()) return;
    this.closeMenu();
    this.formOpening.set(true);
    this.formError.set(null);
    this.productsService
      .getById(product.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (details) => {
          this.formOpening.set(false);
          this.formState.set({ mode: 'edit', product: details });
        },
        error: () => {
          this.formOpening.set(false);
          this.selectedProductId.set(product.id);
          this.selectedProductDetails.set(null);
          this.detailsLoading.set(false);
          this.detailsError.set('Could not load product for editing. Please try again.');
          this.detailsOpen.set(true);
        },
      });
  }

  closeForm(): void {
    if (this.formLoading()) return;
    const createdId = this.pendingCreatedId();
    this.formState.set(null);
    this.formError.set(null);
    this.pendingCreatedId.set(null);
    if (createdId !== null) this.reload$.next();
  }

  saveProduct(value: ProductFormValue): void {
    const state = this.formState();
    if (!state || this.formLoading() || !this.canManage()) return;
    this.formLoading.set(true);
    this.formError.set(null);
    const pendingId = this.pendingCreatedId();
    if (pendingId !== null) {
      this.assignCreatedCategories(pendingId, value.categoryIds);
    } else if (state.mode === 'create') {
      this.productsService
        .create(value.product)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: ({ id }) => {
            if (value.categoryIds.length === 0) {
              this.finishSave();
              return;
            }
            this.pendingCreatedId.set(id);
            this.assignCreatedCategories(id, value.categoryIds);
          },
          error: (error: HttpErrorResponse) =>
            this.failSave(
              error.status === 409
                ? 'SKU already exists. Choose a different SKU.'
                : error.status === 400
                  ? 'Please check the product information.'
                  : 'Could not create product. Please try again.',
            ),
        });
    } else {
      const id = state.product?.id;
      if (id === undefined) {
        this.failSave('Product is no longer available.');
        return;
      }
      const update: UpdateProductRequest = {
        name: value.product.name,
        description: value.product.description,
        unitOfMeasure: value.product.unitOfMeasure,
        unitCost: value.product.unitCost,
        unitPrice: value.product.unitPrice,
        reorderPoint: value.product.reorderPoint,
      };
      this.productsService
        .update(id, update)
        .pipe(
          switchMap(() => this.productsService.assignCategories(id, value.categoryIds)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: () => this.finishSave(),
          error: (error: HttpErrorResponse) =>
            this.failSave(this.mutationMessage(error, 'Could not save product. Please try again.')),
        });
    }
  }

  private assignCreatedCategories(id: number, categoryIds: number[]): void {
    this.productsService
      .assignCategories(id, categoryIds)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finishSave(),
        error: (error: HttpErrorResponse) =>
          this.failSave(
            `Product was created, but its categories could not be assigned. ${this.mutationMessage(error, 'Retry the category assignment.')}`,
          ),
      });
  }

  private finishSave(): void {
    this.formLoading.set(false);
    this.pendingCreatedId.set(null);
    this.formState.set(null);
    this.formError.set(null);
    this.reload$.next();
    if (this.detailsOpen() && this.selectedProductId() !== null) this.retryDetails();
  }

  private failSave(message: string): void {
    this.formLoading.set(false);
    this.formError.set(message);
  }

  requestAction(action: ProductAction, product: ProductListItem | ProductDetails): void {
    if (!this.canManage()) return;
    if (
      (action === 'deactivate' && !product.isActive) ||
      (action === 'reactivate' && product.isActive)
    )
      return;
    const row = this.products().find((item) => item.id === product.id) ?? {
      id: product.id,
      sku: product.sku,
      name: product.name,
      unitPrice: product.unitPrice,
      reorderPoint: product.reorderPoint,
      isActive: product.isActive,
      categoryIds:
        'categoryIds' in product
          ? product.categoryIds
          : product.categories.map((category) => category.categoryId),
    };
    this.closeMenu();
    this.actionError.set(null);
    this.pendingAction.set({ action, product: row });
  }

  closeAction(): void {
    if (!this.actionLoading()) {
      this.pendingAction.set(null);
      this.actionError.set(null);
    }
  }

  confirmAction(): void {
    const pending = this.pendingAction();
    if (!pending || this.actionLoading()) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    const request =
      pending.action === 'deactivate'
        ? this.productsService.deactivate(pending.product.id)
        : this.productsService.reactivate(pending.product.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.closeAction();
        this.reload$.next();
        if (this.detailsOpen() && this.selectedProductId() === pending.product.id)
          this.retryDetails();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoading.set(false);
        this.actionError.set(
          this.mutationMessage(error, `Could not ${pending.action} product. Please try again.`),
        );
      },
    });
  }

  private mutationMessage(error: HttpErrorResponse, fallback: string): string {
    if (error.status === 404) return 'Product or category is no longer available.';
    if (error.status === 409)
      return 'This action conflicts with the current product or category status.';
    if (error.status === 400) return 'Please check the product and selected categories.';
    return fallback;
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    this.categoryPickerOpen.set(false);
    this.closeMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.categoryPickerOpen()) {
      this.categoryPickerOpen.set(false);
      return;
    }
    if (this.pendingAction()) this.closeAction();
    else if (this.formState()) this.closeForm();
    else if (this.detailsOpen()) this.closeDetails();
    else this.closeMenu();
  }
}
