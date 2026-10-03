import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
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
import { CreateCustomerRequest, Customer, UpdateCustomerRequest } from '../models/customer';
import { CustomerFormComponent } from '../customer-form/customer-form';
import { CustomersService } from '../customers.service';

type CustomerStatus = 'all' | 'active' | 'inactive';
type CustomerAction = 'deactivate' | 'reactivate';
type FormState = { mode: 'create' | 'edit'; customer: Customer | null };

@Component({
  selector: 'app-customers-page',
  imports: [CustomerFormComponent, DecimalPipe],
  templateUrl: './customers-page.html',
})
export class CustomersPage implements OnInit {
  private readonly customersService = inject(CustomersService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();
  private readonly detailsSelection$ = new Subject<number | null>();

  readonly canManage = computed(() =>
    this.authService.hasAnyRole(['TenantAdmin', 'WarehouseManager']),
  );
  readonly pageSize = 5;
  readonly customers = signal<Customer[]>([]);
  readonly page = signal(1);
  readonly search = signal('');
  readonly status = signal<CustomerStatus>('all');
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedCustomerId = signal<number | null>(null);
  readonly selectedCustomerDetails = signal<Customer | null>(null);
  readonly detailsOpen = signal(false);
  readonly detailsLoading = signal(false);
  readonly detailsError = signal<string | null>(null);
  readonly menuCustomer = signal<Customer | null>(null);
  readonly menuPosition = signal({ top: 0, left: 0 });
  readonly formState = signal<FormState | null>(null);
  readonly formOpening = signal(false);
  readonly formLoading = signal(false);
  readonly formError = signal<string | null>(null);
  readonly pendingAction = signal<{ action: CustomerAction; customer: Customer } | null>(null);
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
          return this.customersService
            .list({
              searchTerm: this.search().trim(),
              isActive: this.status() === 'all' ? null : this.status() === 'active',
              pageNumber: this.page(),
              pageSize: this.pageSize,
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load customers. Please try again.');
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        this.customers.set(result?.data ?? []);
        this.totalCount.set(result?.totalCount ?? 0);
        if (result && this.page() > this.pageCount()) {
          this.page.set(this.pageCount());
          this.reload$.next();
        }
      });

    this.detailsSelection$
      .pipe(
        switchMap((id) => {
          this.selectedCustomerId.set(id);
          this.selectedCustomerDetails.set(null);
          this.detailsError.set(null);
          this.detailsLoading.set(id !== null);
          if (id === null) return of(null);
          return this.customersService.getById(id).pipe(
            catchError(() => {
              this.detailsError.set('Could not load customer details. Please try again.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((customer) => {
        this.detailsLoading.set(false);
        this.selectedCustomerDetails.set(customer);
      });

    this.reload$.next();
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchInput$.next(value);
  }

  onStatusChange(event: Event): void {
    this.status.set((event.target as HTMLSelectElement).value as CustomerStatus);
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

  openDetails(customer: Customer): void {
    this.closeMenu();
    this.detailsOpen.set(true);
    this.detailsSelection$.next(customer.id);
  }

  retryDetails(): void {
    const id = this.selectedCustomerId();
    if (id !== null) this.detailsSelection$.next(id);
  }

  closeDetails(): void {
    this.closeMenu();
    this.detailsOpen.set(false);
    this.detailsSelection$.next(null);
  }

  openMenu(customer: Customer, event: MouseEvent): void {
    event.stopPropagation();
    if (!this.canManage()) return;
    if (this.menuCustomer()?.id === customer.id) {
      this.closeMenu();
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const width = 176;
    this.menuPosition.set({
      top: Math.max(8, Math.min(rect.bottom + 5, window.innerHeight - 100)),
      left: Math.max(8, Math.min(rect.right - width, window.innerWidth - width - 8)),
    });
    this.menuCustomer.set(customer);
  }

  closeMenu(): void {
    this.menuCustomer.set(null);
  }

  openCreateForm(): void {
    if (!this.canManage()) return;
    this.closeMenu();
    this.formError.set(null);
    this.formState.set({ mode: 'create', customer: null });
  }

  openEditForm(customer: Customer): void {
    if (!this.canManage() || !customer.isActive || this.formOpening()) return;
    this.closeMenu();
    this.formError.set(null);
    const loaded = this.selectedCustomerDetails();
    if (loaded?.id === customer.id) {
      if (loaded.isActive) this.formState.set({ mode: 'edit', customer: loaded });
      else this.detailsOpen.set(true);
      return;
    }
    this.formOpening.set(true);
    this.customersService
      .getById(customer.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (details) => {
          this.formOpening.set(false);
          if (details.isActive) this.formState.set({ mode: 'edit', customer: details });
          else {
            this.selectedCustomerId.set(details.id);
            this.selectedCustomerDetails.set(details);
            this.detailsLoading.set(false);
            this.detailsError.set(null);
            this.detailsOpen.set(true);
            this.reload$.next();
          }
        },
        error: () => {
          this.formOpening.set(false);
          this.selectedCustomerId.set(customer.id);
          this.selectedCustomerDetails.set(null);
          this.detailsLoading.set(false);
          this.detailsError.set('Could not load customer for editing. Please try again.');
          this.detailsOpen.set(true);
        },
      });
  }

  closeForm(): void {
    if (this.formLoading()) return;
    this.formState.set(null);
    this.formError.set(null);
  }

  saveCustomer(value: CreateCustomerRequest): void {
    const state = this.formState();
    if (!state || this.formLoading() || !this.canManage()) return;
    this.formLoading.set(true);
    this.formError.set(null);
    if (state.mode === 'create') {
      this.customersService
        .create(value)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => this.finishSave(),
          error: (error: HttpErrorResponse) =>
            this.failSave(
              error.status === 409
                ? 'Customer code already exists. Choose a different code.'
                : error.status === 400
                  ? 'Please check the customer information.'
                  : 'Could not create customer. Please try again.',
            ),
        });
    } else {
      const id = state.customer?.id;
      if (id === undefined) {
        this.failSave('Customer is no longer available.');
        return;
      }
      const request: UpdateCustomerRequest = {
        name: value.name,
        contactEmail: value.contactEmail,
        phoneNumber: value.phoneNumber,
        address: value.address,
        creditLimit: value.creditLimit,
      };
      this.customersService
        .update(id, request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => this.finishSave(id),
          error: (error: HttpErrorResponse) =>
            this.failSave(
              this.mutationMessage(error, 'Could not save customer. Please try again.'),
            ),
        });
    }
  }

  private finishSave(id?: number): void {
    this.formLoading.set(false);
    this.formState.set(null);
    this.formError.set(null);
    this.reload$.next();
    if (id !== undefined && this.detailsOpen() && this.selectedCustomerId() === id)
      this.retryDetails();
  }

  private failSave(message: string): void {
    this.formLoading.set(false);
    this.formError.set(message);
  }

  requestAction(action: CustomerAction, customer: Customer): void {
    if (
      !this.canManage() ||
      (action === 'deactivate' && !customer.isActive) ||
      (action === 'reactivate' && customer.isActive)
    )
      return;
    this.closeMenu();
    this.actionError.set(null);
    this.pendingAction.set({ action, customer });
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
        ? this.customersService.deactivate(pending.customer.id)
        : this.customersService.reactivate(pending.customer.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.closeAction();
        this.reload$.next();
        if (this.detailsOpen() && this.selectedCustomerId() === pending.customer.id) {
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
          this.mutationMessage(error, `Could not ${pending.action} customer. Please try again.`),
        );
      },
    });
  }

  private mutationMessage(error: HttpErrorResponse, fallback: string): string {
    if (error.status === 404) return 'Customer is no longer available.';
    if (error.status === 409) return 'Customer status has changed. Refresh and try again.';
    if (error.status === 400) return 'Please check the customer information.';
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
    else if (this.menuCustomer()) this.closeMenu();
    else if (this.detailsOpen()) this.closeDetails();
  }
}
