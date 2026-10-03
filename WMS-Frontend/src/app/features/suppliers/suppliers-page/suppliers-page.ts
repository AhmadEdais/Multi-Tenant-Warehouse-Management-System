import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
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
import { CreateSupplierRequest, Supplier, UpdateSupplierRequest } from '../models/supplier';
import { SupplierFormComponent } from '../supplier-form/supplier-form';
import { SuppliersService } from '../suppliers.service';

type SupplierStatus = 'all' | 'active' | 'inactive';
type SupplierAction = 'deactivate' | 'reactivate';
type FormState = { mode: 'create' | 'edit'; supplier: Supplier | null };

@Component({
  selector: 'app-suppliers-page',
  imports: [SupplierFormComponent],
  templateUrl: './suppliers-page.html',
})
export class SuppliersPage implements OnInit {
  private readonly suppliersService = inject(SuppliersService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();
  private readonly detailsSelection$ = new Subject<number | null>();

  readonly canManage = computed(() =>
    this.authService.hasAnyRole(['TenantAdmin', 'WarehouseManager']),
  );
  readonly pageSize = 5;
  readonly suppliers = signal<Supplier[]>([]);
  readonly page = signal(1);
  readonly search = signal('');
  readonly status = signal<SupplierStatus>('all');
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedSupplierId = signal<number | null>(null);
  readonly selectedSupplierDetails = signal<Supplier | null>(null);
  readonly detailsOpen = signal(false);
  readonly detailsLoading = signal(false);
  readonly detailsError = signal<string | null>(null);
  readonly menuSupplier = signal<Supplier | null>(null);
  readonly menuPosition = signal({ top: 0, left: 0 });
  readonly formState = signal<FormState | null>(null);
  readonly formOpening = signal(false);
  readonly formLoading = signal(false);
  readonly formError = signal<string | null>(null);
  readonly pendingAction = signal<{ action: SupplierAction; supplier: Supplier } | null>(null);
  readonly actionLoading = signal(false);
  readonly actionError = signal<string | null>(null);

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
        tap(() => this.page.set(1)),
      ),
      this.reload$,
    )
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.suppliersService
            .list({
              searchTerm: this.search().trim(),
              isActive: this.status() === 'all' ? null : this.status() === 'active',
              pageNumber: this.page(),
              pageSize: this.pageSize,
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load suppliers. Please try again.');
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        this.suppliers.set(result?.data ?? []);
        this.totalCount.set(result?.totalCount ?? 0);
        if (result && this.page() > this.pageCount()) {
          this.page.set(this.pageCount());
          this.reload$.next();
        }
      });

    this.detailsSelection$
      .pipe(
        switchMap((id) => {
          this.selectedSupplierId.set(id);
          this.selectedSupplierDetails.set(null);
          this.detailsError.set(null);
          this.detailsLoading.set(id !== null);
          if (id === null) return of(null);
          return this.suppliersService.getById(id).pipe(
            catchError(() => {
              this.detailsError.set('Could not load supplier details. Please try again.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((supplier) => {
        this.detailsLoading.set(false);
        this.selectedSupplierDetails.set(supplier);
      });

    this.reload$.next();
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchInput$.next(value);
  }

  onStatusChange(event: Event): void {
    this.status.set((event.target as HTMLSelectElement).value as SupplierStatus);
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

  openDetails(supplier: Supplier): void {
    this.closeMenu();
    this.detailsOpen.set(true);
    this.detailsSelection$.next(supplier.id);
  }

  retryDetails(): void {
    const id = this.selectedSupplierId();
    if (id !== null) this.detailsSelection$.next(id);
  }

  closeDetails(): void {
    this.closeMenu();
    this.detailsOpen.set(false);
    this.detailsSelection$.next(null);
  }

  openMenu(supplier: Supplier, event: MouseEvent): void {
    event.stopPropagation();
    if (!this.canManage()) return;
    if (this.menuSupplier()?.id === supplier.id) {
      this.closeMenu();
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const width = 176;
    this.menuPosition.set({
      top: Math.max(8, Math.min(rect.bottom + 5, window.innerHeight - 100)),
      left: Math.max(8, Math.min(rect.right - width, window.innerWidth - width - 8)),
    });
    this.menuSupplier.set(supplier);
  }

  closeMenu(): void {
    this.menuSupplier.set(null);
  }

  openCreateForm(): void {
    if (!this.canManage()) return;
    this.closeMenu();
    this.formError.set(null);
    this.formState.set({ mode: 'create', supplier: null });
  }

  openEditForm(supplier: Supplier): void {
    if (!this.canManage() || !supplier.isActive || this.formOpening()) return;
    this.closeMenu();
    this.formError.set(null);
    const loaded = this.selectedSupplierDetails();
    if (loaded?.id === supplier.id) {
      if (loaded.isActive) this.formState.set({ mode: 'edit', supplier: loaded });
      else this.detailsOpen.set(true);
      return;
    }
    this.formOpening.set(true);
    this.suppliersService
      .getById(supplier.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (details) => {
          this.formOpening.set(false);
          if (details.isActive) this.formState.set({ mode: 'edit', supplier: details });
          else {
            this.selectedSupplierId.set(details.id);
            this.selectedSupplierDetails.set(details);
            this.detailsLoading.set(false);
            this.detailsError.set(null);
            this.detailsOpen.set(true);
            this.reload$.next();
          }
        },
        error: () => {
          this.formOpening.set(false);
          this.selectedSupplierId.set(supplier.id);
          this.selectedSupplierDetails.set(null);
          this.detailsLoading.set(false);
          this.detailsError.set('Could not load supplier for editing. Please try again.');
          this.detailsOpen.set(true);
        },
      });
  }

  closeForm(): void {
    if (this.formLoading()) return;
    this.formState.set(null);
    this.formError.set(null);
  }

  saveSupplier(value: CreateSupplierRequest): void {
    const state = this.formState();
    if (!state || this.formLoading() || !this.canManage()) return;
    this.formLoading.set(true);
    this.formError.set(null);
    if (state.mode === 'create') {
      this.suppliersService
        .create(value)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => this.finishSave(),
          error: (error: HttpErrorResponse) =>
            this.failSave(
              error.status === 409
                ? 'Supplier code already exists. Choose a different code.'
                : error.status === 400
                  ? 'Please check the supplier information.'
                  : 'Could not create supplier. Please try again.',
            ),
        });
    } else {
      const id = state.supplier?.id;
      if (id === undefined) {
        this.failSave('Supplier is no longer available.');
        return;
      }
      const request: UpdateSupplierRequest = {
        name: value.name,
        contactEmail: value.contactEmail,
        phoneNumber: value.phoneNumber,
        address: value.address,
      };
      this.suppliersService
        .update(id, request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => this.finishSave(id),
          error: (error: HttpErrorResponse) =>
            this.failSave(
              this.mutationMessage(error, 'Could not save supplier. Please try again.'),
            ),
        });
    }
  }

  private finishSave(id?: number): void {
    this.formLoading.set(false);
    this.formState.set(null);
    this.formError.set(null);
    this.reload$.next();
    if (id !== undefined && this.detailsOpen() && this.selectedSupplierId() === id)
      this.retryDetails();
  }

  private failSave(message: string): void {
    this.formLoading.set(false);
    this.formError.set(message);
  }

  requestAction(action: SupplierAction, supplier: Supplier): void {
    if (
      !this.canManage() ||
      (action === 'deactivate' && !supplier.isActive) ||
      (action === 'reactivate' && supplier.isActive)
    )
      return;
    this.closeMenu();
    this.actionError.set(null);
    this.pendingAction.set({ action, supplier });
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
        ? this.suppliersService.deactivate(pending.supplier.id)
        : this.suppliersService.reactivate(pending.supplier.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.closeAction();
        this.reload$.next();
        if (this.detailsOpen() && this.selectedSupplierId() === pending.supplier.id) {
          const nowActive = pending.action === 'reactivate';
          if (
            (this.status() === 'active' && !nowActive) ||
            (this.status() === 'inactive' && nowActive)
          )
            this.closeDetails();
          else this.retryDetails();
        }
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoading.set(false);
        this.actionError.set(
          this.mutationMessage(error, `Could not ${pending.action} supplier. Please try again.`),
        );
      },
    });
  }

  private mutationMessage(error: HttpErrorResponse, fallback: string): string {
    if (error.status === 404) return 'Supplier is no longer available.';
    if (error.status === 409) return 'Supplier status has changed. Refresh and try again.';
    if (error.status === 400) return 'Please check the supplier information.';
    return fallback;
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    this.closeMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.pendingAction()) this.closeAction();
    else if (this.formState()) this.closeForm();
    else if (this.menuSupplier()) this.closeMenu();
    else if (this.detailsOpen()) this.closeDetails();
  }
}
