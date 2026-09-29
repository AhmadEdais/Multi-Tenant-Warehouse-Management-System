import { DatePipe } from '@angular/common';
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
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
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
import { CreateWarehouseRequest, UpdateWarehouseRequest, Warehouse } from '../models/warehouse';
import { WarehousesService } from '../warehouses.service';

type WarehouseStatus = 'all' | 'active' | 'inactive';

const requiredText = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && !control.value.trim() ? { required: true } : null;

@Component({
  selector: 'app-warehouses-page',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './warehouses-page.html',
  styleUrl: './warehouses-page.css',
})
export class WarehousesPage implements OnInit {
  private readonly warehousesService = inject(WarehousesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  readonly pageSize = 5;
  readonly warehouses = signal<Warehouse[]>([]);
  readonly page = signal(1);
  readonly search = signal('');
  readonly status = signal<WarehouseStatus>('all');
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedWarehouse = signal<Warehouse | null>(null);
  readonly viewedWarehouse = signal<Warehouse | null>(null);
  readonly showCreateModal = signal(false);
  readonly showEditModal = signal(false);
  readonly pendingAction = signal(false);
  readonly createLoading = signal(false);
  readonly createError = signal<string | null>(null);
  readonly editLoading = signal(false);
  readonly editError = signal<string | null>(null);
  readonly actionLoading = signal(false);
  readonly actionError = signal<string | null>(null);
  readonly menuPosition = signal({ top: 0, left: 0 });

  readonly createWarehouseForm = new FormGroup({
    code: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(20)],
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(200)],
    }),
    address: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(500)],
    }),
  });
  readonly editWarehouseForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(200)],
    }),
    address: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(500)],
    }),
  });

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
          if (!this.showEditModal() && !this.pendingAction()) this.closeMenu();
          return this.warehousesService
            .list({
              pageNumber: this.page(),
              pageSize: this.pageSize,
              search: this.search().trim(),
              isActive: this.status() === 'all' ? null : this.status() === 'active',
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load warehouses. Please try again.');
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        if (!result) {
          this.warehouses.set([]);
          this.totalCount.set(0);
          return;
        }
        this.warehouses.set(result.data);
        this.totalCount.set(result.totalCount);
        if (this.page() > this.pageCount()) {
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

  onStatusChange(event: Event): void {
    this.status.set((event.target as HTMLSelectElement).value as WarehouseStatus);
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

  openCreateModal(): void {
    this.closeMenu();
    this.createWarehouseForm.reset();
    this.createError.set(null);
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    if (this.createLoading()) return;
    this.showCreateModal.set(false);
    this.createWarehouseForm.reset();
    this.createError.set(null);
  }

  createWarehouse(): void {
    if (this.createLoading()) return;
    if (this.createWarehouseForm.invalid) {
      this.createWarehouseForm.markAllAsTouched();
      return;
    }
    const values = this.createWarehouseForm.getRawValue();
    const request: CreateWarehouseRequest = {
      code: values.code.trim(),
      name: values.name.trim(),
      address: values.address.trim() || null,
    };
    this.createLoading.set(true);
    this.createError.set(null);
    this.warehousesService
      .create(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.createLoading.set(false);
          this.closeCreateModal();
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.createLoading.set(false);
          this.createError.set(
            error.status === 409
              ? 'Warehouse code already exists. Choose a different code.'
              : error.status === 400
                ? 'Please check the warehouse information and try again.'
                : 'Could not create warehouse. Please try again.',
          );
        },
      });
  }

  openViewModal(warehouse: Warehouse): void {
    this.closeMenu();
    this.viewedWarehouse.set(warehouse);
  }

  closeViewModal(): void {
    this.viewedWarehouse.set(null);
  }

  openMenu(warehouse: Warehouse, event: MouseEvent): void {
    event.stopPropagation();
    if (this.selectedWarehouse()?.id === warehouse.id) {
      this.closeMenu();
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const menuWidth = 176;
    this.menuPosition.set({
      top: rect.bottom + 6 + 104 > window.innerHeight ? rect.top - 110 : rect.bottom + 6,
      left: Math.max(8, Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 8)),
    });
    this.selectedWarehouse.set(warehouse);
    this.actionError.set(null);
  }

  openEditModal(): void {
    const warehouse = this.selectedWarehouse();
    if (!warehouse || !warehouse.isActive) return;
    this.editWarehouseForm.reset({ name: warehouse.name, address: warehouse.address ?? '' });
    this.editError.set(null);
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    if (this.editLoading()) return;
    this.showEditModal.set(false);
    this.editWarehouseForm.reset();
    this.editError.set(null);
    this.closeMenu();
  }

  updateWarehouse(): void {
    const warehouse = this.selectedWarehouse();
    if (!warehouse || !warehouse.isActive || this.editLoading()) return;
    if (this.editWarehouseForm.invalid) {
      this.editWarehouseForm.markAllAsTouched();
      return;
    }
    const values = this.editWarehouseForm.getRawValue();
    const request: UpdateWarehouseRequest = {
      name: values.name.trim(),
      address: values.address.trim() || null,
    };
    this.editLoading.set(true);
    this.editError.set(null);
    this.warehousesService
      .update(warehouse.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.editLoading.set(false);
          this.closeEditModal();
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.editLoading.set(false);
          this.editError.set(
            error.status === 400
              ? 'Please check the warehouse information and try again.'
              : error.status === 409
                ? 'Inactive warehouses cannot be edited.'
                : error.status === 404
                  ? 'Warehouse is no longer available.'
                  : 'Could not update warehouse. Please try again.',
          );
        },
      });
  }

  requestDeactivate(): void {
    if (!this.selectedWarehouse()?.isActive) return;
    this.pendingAction.set(true);
    this.actionError.set(null);
  }

  confirmDeactivate(): void {
    const warehouse = this.selectedWarehouse();
    if (!warehouse || !this.pendingAction() || this.actionLoading()) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    this.warehousesService
      .deactivate(warehouse.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.actionLoading.set(false);
          this.closeMenu();
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.actionLoading.set(false);
          this.actionError.set(
            error.status === 404
              ? 'Warehouse is no longer available.'
              : error.status === 409
                ? 'Warehouse is already inactive.'
                : `Could not deactivate ${warehouse.name}. Please try again.`,
          );
        },
      });
  }

  closeMenu(): void {
    if (this.editLoading() || this.actionLoading()) return;
    this.pendingAction.set(false);
    this.selectedWarehouse.set(null);
    this.actionError.set(null);
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    if (!this.pendingAction() && !this.showEditModal()) this.closeMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeCreateModal();
    this.closeViewModal();
    if (this.showEditModal()) this.closeEditModal();
    else this.closeMenu();
  }
}
