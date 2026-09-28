import { DatePipe } from '@angular/common';
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
import { Tenant } from '../models/tenant';
import { TenantsService } from '../tenants.service';

type TenantStatus = 'all' | 'active' | 'suspended';
type TenantAction = 'suspend' | 'reactivate';

@Component({
  selector: 'app-tenants-page',
  imports: [DatePipe],
  templateUrl: './tenants-page.html',
  styleUrl: './tenants-page.css',
})
export class TenantsPage implements OnInit {
  private readonly tenantsService = inject(TenantsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  readonly pageSize = 5;
  readonly tenants = signal<Tenant[]>([]);
  readonly page = signal(1);
  readonly search = signal('');
  readonly status = signal<TenantStatus>('all');
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly selectedTenant = signal<Tenant | null>(null);
  readonly pendingAction = signal<TenantAction | null>(null);
  readonly actionLoading = signal(false);
  readonly menuPosition = signal({ top: 0, left: 0 });

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
          this.closeMenu();

          return this.tenantsService
            .list({
              pageNumber: this.page(),
              pageSize: this.pageSize,
              search: this.search().trim(),
              isActive: this.status() === 'all' ? null : this.status() === 'active',
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load tenants. Please try again.');
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        if (!result) {
          this.tenants.set([]);
          this.totalCount.set(0);
          return;
        }

        this.tenants.set(result.data);
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
    this.status.set((event.target as HTMLSelectElement).value as TenantStatus);
    this.page.set(1);
    this.reload$.next();
  }

  goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.pageCount() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.reload$.next();
  }

  retry(): void {
    this.reload$.next();
  }

  openMenu(tenant: Tenant, event: MouseEvent): void {
    event.stopPropagation();
    if (this.selectedTenant()?.id === tenant.id) {
      this.closeMenu();
      return;
    }

    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const menuWidth = 176;
    this.menuPosition.set({
      top: rect.bottom + 6 + 104 > window.innerHeight ? rect.top - 110 : rect.bottom + 6,
      left: Math.max(8, Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 8)),
    });
    this.selectedTenant.set(tenant);
    this.actionError.set(null);
  }

  requestAction(action: TenantAction): void {
    const tenant = this.selectedTenant();
    if (!tenant || (action === 'suspend' ? !tenant.isActive : tenant.isActive)) {
      return;
    }
    this.pendingAction.set(action);
    this.actionError.set(null);
  }

  confirmAction(): void {
    const tenant = this.selectedTenant();
    const action = this.pendingAction();
    if (!tenant || !action || this.actionLoading()) {
      return;
    }

    this.actionLoading.set(true);
    this.actionError.set(null);
    const request =
      action === 'suspend'
        ? this.tenantsService.suspend(tenant.id)
        : this.tenantsService.reactivate(tenant.id);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.pendingAction.set(null);
        this.closeMenu();
        this.reload$.next();
      },
      error: () => {
        this.actionLoading.set(false);
        this.actionError.set(`Could not ${action} ${tenant.name}. Please try again.`);
      },
    });
  }

  closeMenu(): void {
    if (this.actionLoading()) {
      return;
    }
    this.pendingAction.set(null);
    this.selectedTenant.set(null);
    this.actionError.set(null);
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    if (!this.pendingAction()) {
      this.closeMenu();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
  }
}
