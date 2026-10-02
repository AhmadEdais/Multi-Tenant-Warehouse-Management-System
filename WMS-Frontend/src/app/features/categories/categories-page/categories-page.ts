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
import { catchError, EMPTY, Observable, of, Subject, switchMap } from 'rxjs';
import { AuthService } from '../../auth/auth.services';
import { CategoryFormComponent } from '../category-form/category-form';
import { CategoryTreeNode } from '../category-tree-node/category-tree-node';
import { CategoriesService } from '../categories.service';
import { CategoryDetails, CategoryNode, CreateCategoryRequest } from '../models/category';

type CategoryStatus = 'all' | 'active' | 'inactive';
type FormState = {
  mode: 'add' | 'edit';
  category: CategoryNode | null;
  lockedParent: CategoryNode | null;
};
type ActionState = { action: 'deactivate' | 'reactivate'; category: CategoryNode };

@Component({
  selector: 'app-categories-page',
  imports: [CategoryTreeNode, CategoryFormComponent],
  templateUrl: './categories-page.html',
  styleUrl: './categories-page.css',
})
export class CategoriesPage implements OnInit {
  private readonly categoriesService = inject(CategoriesService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private readonly detailsSelection$ = new Subject<CategoryNode | null>();

  readonly canManage = computed(() => {
    const roles = this.authService.currentUser()?.roles ?? [];
    return roles.includes('TenantAdmin') || roles.includes('WarehouseManager');
  });
  readonly categoryTree = signal<CategoryNode[]>([]);
  readonly treeLoading = signal(false);
  readonly treeError = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly status = signal<CategoryStatus>('all');
  private readonly normalizeStatusForRole = effect(() => {
    if (!this.canManage() && this.status() === 'inactive') this.status.set('all');
  });
  readonly expandedIds = signal<Set<number>>(new Set());
  readonly selectedCategoryNode = signal<CategoryNode | null>(null);
  readonly selectedCategoryDetails = signal<CategoryDetails | null>(null);
  readonly detailsLoading = signal(false);
  readonly detailsError = signal<string | null>(null);
  readonly contextCategory = signal<CategoryNode | null>(null);
  readonly contextPosition = signal({ top: 0, left: 0 });
  readonly formState = signal<FormState | null>(null);
  readonly formLoading = signal(false);
  readonly formError = signal<string | null>(null);
  readonly pendingAction = signal<ActionState | null>(null);
  readonly actionLoading = signal(false);
  readonly actionError = signal<string | null>(null);

  readonly isSearching = computed(() => this.searchTerm().trim().length > 0);
  readonly filteredTree = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const status = this.status();
    return term || status !== 'all'
      ? this.filterCategories(this.categoryTree(), term, status)
      : this.categoryTree();
  });

  ngOnInit(): void {
    this.reload$
      .pipe(
        switchMap(() => {
          this.treeLoading.set(true);
          this.treeError.set(null);
          return this.categoriesService.getTree().pipe(
            catchError(() => {
              this.treeError.set('Could not load categories. Please try again.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((tree) => {
        this.treeLoading.set(false);
        this.categoryTree.set(tree ?? []);
      });

    this.detailsSelection$
      .pipe(
        switchMap((node) => {
          this.selectedCategoryNode.set(node);
          this.selectedCategoryDetails.set(null);
          this.detailsError.set(null);
          this.detailsLoading.set(node !== null);
          if (!node) return EMPTY;
          return this.categoriesService.getById(node.id).pipe(
            catchError(() => {
              this.detailsLoading.set(false);
              this.detailsError.set('Could not load category details. Please try again.');
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((details) => {
        this.detailsLoading.set(false);
        this.selectedCategoryDetails.set(details);
        this.selectedCategoryNode.update((node) =>
          node?.id === details.id
            ? {
                ...node,
                name: details.name,
                parentCategoryId: details.parentCategoryId,
                isActive: details.isActive,
              }
            : node,
        );
      });

    this.reload$.next();
  }

  retryTree(): void {
    this.reload$.next();
  }
  selectCategory(node: CategoryNode): void {
    this.closeContextMenu();
    this.detailsSelection$.next(node);
  }
  retryDetails(): void {
    const node = this.selectedCategoryNode();
    if (node) this.detailsSelection$.next(node);
  }
  onSearchInput(event: Event): void {
    this.closeContextMenu();
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }
  onStatusChange(event: Event): void {
    this.closeContextMenu();
    const selectedStatus = (event.target as HTMLSelectElement).value as CategoryStatus;
    this.status.set(selectedStatus === 'inactive' && !this.canManage() ? 'all' : selectedStatus);
  }
  toggleExpanded(id: number): void {
    if (this.isSearching()) return;
    this.expandedIds.update((current) => {
      const updated = new Set(current);
      if (updated.has(id)) updated.delete(id);
      else updated.add(id);
      return updated;
    });
  }

  private filterCategories(
    nodes: CategoryNode[],
    term: string,
    status: CategoryStatus,
  ): CategoryNode[] {
    return nodes
      .map((node) => {
        const children = this.filterCategories(node.children, term, status);
        const matches =
          (!term || node.name.toLowerCase().includes(term)) &&
          (status === 'all' || node.isActive === (status === 'active'));
        return matches || children.length ? { ...node, children } : null;
      })
      .filter((node): node is CategoryNode => node !== null);
  }

  openContextMenu(event: { node: CategoryNode; x: number; y: number }): void {
    if (!this.canManage()) return;
    this.contextCategory.set(event.node);
    this.contextPosition.set({
      top: Math.max(8, Math.min(event.y, window.innerHeight - 150)),
      left: Math.max(8, Math.min(event.x, window.innerWidth - 184)),
    });
  }
  openSelectedActions(event: MouseEvent): void {
    event.stopPropagation();
    const node = this.selectedCategoryNode();
    if (!node || !this.canManage()) return;
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.openContextMenu({ node, x: rect.right - 176, y: rect.bottom + 4 });
  }
  closeContextMenu(): void {
    this.contextCategory.set(null);
  }

  openAddCategory(): void {
    if (!this.canManage() || this.treeLoading() || this.treeError()) return;
    this.closeContextMenu();
    this.formError.set(null);
    this.formState.set({ mode: 'add', category: null, lockedParent: null });
  }
  openAddChild(): void {
    const parent = this.contextCategory();
    if (!this.canManage() || !parent?.isActive) return;
    this.formError.set(null);
    this.formState.set({ mode: 'add', category: null, lockedParent: parent });
    this.closeContextMenu();
  }
  openEditCategory(category: CategoryNode | null = this.contextCategory()): void {
    if (!this.canManage() || !category?.isActive) return;
    this.formError.set(null);
    this.formState.set({ mode: 'edit', category, lockedParent: null });
    this.closeContextMenu();
  }
  closeCategoryForm(): void {
    if (this.formLoading()) return;
    this.formState.set(null);
    this.formError.set(null);
  }
  saveCategory(value: CreateCategoryRequest): void {
    const state = this.formState();
    if (!state || !this.canManage() || this.formLoading()) return;
    this.formLoading.set(true);
    this.formError.set(null);
    const request: Observable<unknown> =
      state.mode === 'add'
        ? this.categoriesService.create(value)
        : this.categoriesService.update(state.category!.id, value);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.formLoading.set(false);
        this.closeCategoryForm();
        this.reload$.next();
        if (this.selectedCategoryNode()) this.retryDetails();
      },
      error: (error: HttpErrorResponse) => {
        this.formLoading.set(false);
        this.formError.set(this.categoryError(error, 'Could not save category. Please try again.'));
      },
    });
  }

  requestAction(
    action: 'deactivate' | 'reactivate',
    category: CategoryNode | null = this.contextCategory(),
  ): void {
    if (!this.canManage() || !category || category.isActive !== (action === 'deactivate')) return;
    this.pendingAction.set({ action, category });
    this.actionError.set(null);
    this.closeContextMenu();
  }
  closeAction(): void {
    if (this.actionLoading()) return;
    this.pendingAction.set(null);
    this.actionError.set(null);
  }
  confirmAction(): void {
    const pending = this.pendingAction();
    if (!pending || !this.canManage() || this.actionLoading()) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    const request =
      pending.action === 'deactivate'
        ? this.categoriesService.deactivate(pending.category.id)
        : this.categoriesService.reactivate(pending.category.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.closeAction();
        this.reload$.next();
        if (this.selectedCategoryNode()) this.retryDetails();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoading.set(false);
        this.actionError.set(
          this.categoryError(error, `Could not ${pending.action} category. Please try again.`),
        );
      },
    });
  }

  private categoryError(error: HttpErrorResponse, fallback: string): string {
    const detail = typeof error.error?.detail === 'string' ? error.error.detail : '';
    const expected = [
      'The selected parent category is inactive.',
      'A category cannot be its own parent.',
      'A category cannot be moved under one of its descendants.',
      'This category has active child categories and cannot be deactivated.',
      'Category is already active.',
      'Category is already inactive.',
      'Inactive categories cannot be edited.',
    ];
    if (error.status === 409 && expected.includes(detail)) return detail;
    if (error.status === 404)
      return 'This category or its parent is no longer available. Refresh and try again.';
    if (error.status === 400) return 'Please check the category information and try again.';
    return fallback;
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    this.closeContextMenu();
  }
  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeContextMenu();
    this.closeCategoryForm();
    this.closeAction();
  }
}
