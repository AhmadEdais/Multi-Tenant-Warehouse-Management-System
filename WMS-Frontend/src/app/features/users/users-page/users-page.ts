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
import { AssignableRole, CreateUserRequest, User } from '../models/user';
import { UsersService } from '../users.service';
import { AuthService } from '../../auth/auth.services';

type UserStatus = 'all' | 'active' | 'inactive';
type UserAction = 'deactivate' | 'reactivate';

const requiredText = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && !control.value.trim() ? { required: true } : null;
const atLeastOneRole = (control: AbstractControl): ValidationErrors | null =>
  Array.isArray(control.value) && control.value.length > 0 ? null : { required: true };

@Component({
  selector: 'app-users-page',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './users-page.html',
  styleUrl: './users-page.css',
})
export class UsersPage implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private readonly reload$ = new Subject<void>();

  readonly pageSize = 5;
  readonly users = signal<User[]>([]);
  readonly page = signal(1);
  readonly search = signal('');
  readonly status = signal<UserStatus>('all');
  readonly roleFilter = signal('all');
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly roles = signal<AssignableRole[]>([]);
  readonly rolesLoading = signal(false);
  readonly rolesError = signal<string | null>(null);
  readonly selectedUser = signal<User | null>(null);
  readonly viewedUser = signal<User | null>(null);
  readonly showCreateUserModal = signal(false);
  readonly showEditRolesModal = signal(false);
  readonly pendingAction = signal<UserAction | null>(null);
  readonly actionLoading = signal(false);
  readonly actionError = signal<string | null>(null);
  readonly createLoading = signal(false);
  readonly createError = signal<string | null>(null);
  readonly roleUpdateLoading = signal(false);
  readonly roleUpdateError = signal<string | null>(null);
  readonly menuPosition = signal({ top: 0, left: 0 });
  readonly currentUser = this.authService.currentUser;

  readonly createUserForm = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(200)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.email, Validators.maxLength(256)],
    }),
    initialPassword: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.minLength(5)],
    }),
    roleIds: new FormControl<number[]>([], { nonNullable: true, validators: [atLeastOneRole] }),
  });
  readonly editRolesForm = new FormGroup({
    roleIds: new FormControl<number[]>([], { nonNullable: true, validators: [atLeastOneRole] }),
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
          if (!this.showEditRolesModal() && !this.pendingAction()) this.closeMenu();
          return this.usersService
            .list({
              pageNumber: this.page(),
              pageSize: this.pageSize,
              searchTerm: this.search().trim(),
              isActive: this.status() === 'all' ? null : this.status() === 'active',
              role: this.roleFilter() === 'all' ? null : this.roleFilter(),
            })
            .pipe(
              catchError(() => {
                this.error.set('Could not load users. Please try again.');
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        if (!result) {
          this.users.set([]);
          this.totalCount.set(0);
          return;
        }
        this.users.set(result.data);
        this.totalCount.set(result.totalCount);
        if (this.page() > this.pageCount()) {
          this.page.set(this.pageCount());
          this.reload$.next();
        }
      });

    this.reload$.next();
    this.loadRoles();
  }

  loadRoles(): void {
    if (this.rolesLoading()) return;
    this.rolesLoading.set(true);
    this.rolesError.set(null);
    this.usersService
      .assignableRoles()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (roles) => {
          const assignable = roles.filter((role) => role.name !== 'SystemAdmin');
          this.roles.set(assignable);
          if (!assignable.length) {
            this.rolesError.set('No assignable roles are available.');
          }
          this.rolesLoading.set(false);
        },
        error: () => {
          this.rolesLoading.set(false);
          this.rolesError.set('Could not load roles. Please try again.');
        },
      });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchInput$.next(value);
  }

  onStatusChange(event: Event): void {
    this.status.set((event.target as HTMLSelectElement).value as UserStatus);
    this.page.set(1);
    this.reload$.next();
  }

  onRoleChange(event: Event): void {
    this.roleFilter.set((event.target as HTMLSelectElement).value);
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

  roleLabel(name: string): string {
    return name.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  toggleRole(control: FormControl<number[]>, id: number, checked: boolean): void {
    const selected = control.value;
    control.setValue(checked ? [...selected, id] : selected.filter((roleId) => roleId !== id));
    control.markAsTouched();
  }

  openCreateModal(): void {
    this.closeMenu();
    this.createUserForm.reset();
    this.createError.set(null);
    this.showCreateUserModal.set(true);
  }

  closeCreateModal(): void {
    if (this.createLoading()) return;
    this.showCreateUserModal.set(false);
    this.createUserForm.reset();
    this.createError.set(null);
  }

  createUser(): void {
    if (this.createLoading()) return;
    if (this.createUserForm.invalid || this.rolesError() || this.rolesLoading()) {
      this.createUserForm.markAllAsTouched();
      return;
    }
    const values = this.createUserForm.getRawValue();
    const request: CreateUserRequest = {
      fullName: values.fullName.trim(),
      email: values.email.trim(),
      initialPassword: values.initialPassword,
      roleIds: values.roleIds,
    };
    this.createLoading.set(true);
    this.createError.set(null);
    this.usersService
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
              ? 'Email already exists. Use a different email.'
              : error.status === 400 || error.status === 404 || error.status === 401
                ? 'Please check the user details and selected roles.'
                : 'Could not create user. Please try again.',
          );
        },
      });
  }

  openViewModal(user: User): void {
    this.closeMenu();
    this.viewedUser.set(user);
  }

  closeViewModal(): void {
    this.viewedUser.set(null);
  }

  openMenu(user: User, event: MouseEvent): void {
    event.stopPropagation();
    if (this.selectedUser()?.id === user.id) {
      this.closeMenu();
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const menuWidth = 176;
    this.menuPosition.set({
      top: rect.bottom + 6 + 148 > window.innerHeight ? rect.top - 154 : rect.bottom + 6,
      left: Math.max(8, Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 8)),
    });
    this.selectedUser.set(user);
    this.actionError.set(null);
  }

  openEditRolesModal(): void {
    const user = this.selectedUser();
    if (!user || user.id === this.currentUser()?.id || this.rolesLoading() || this.rolesError())
      return;
    this.editRolesForm.reset({
      roleIds: this.roles()
        .filter((role) => user.roles.includes(role.name))
        .map((role) => role.id),
    });
    this.roleUpdateError.set(null);
    this.showEditRolesModal.set(true);
  }

  closeEditRolesModal(): void {
    if (this.roleUpdateLoading()) return;
    this.showEditRolesModal.set(false);
    this.editRolesForm.reset();
    this.roleUpdateError.set(null);
    this.closeMenu();
  }

  updateRoles(): void {
    const user = this.selectedUser();
    if (!user || this.roleUpdateLoading()) return;
    if (this.editRolesForm.invalid) {
      this.editRolesForm.markAllAsTouched();
      return;
    }
    this.roleUpdateLoading.set(true);
    this.roleUpdateError.set(null);
    this.usersService
      .replaceRoles(user.id, this.editRolesForm.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.roleUpdateLoading.set(false);
          this.closeEditRolesModal();
          this.reload$.next();
        },
        error: (error: HttpErrorResponse) => {
          this.roleUpdateLoading.set(false);
          const detail = typeof error.error?.detail === 'string' ? error.error.detail : '';
          this.roleUpdateError.set(
            error.status === 409 && detail.includes('last active TenantAdmin')
              ? 'The last active Tenant Admin must keep that role.'
              : error.status === 409 && detail.includes('own roles')
                ? 'You cannot change your own roles.'
                : error.status === 400 || error.status === 401 || error.status === 404
                  ? 'Please check the selected roles and try again.'
                  : 'Could not update roles. Please try again.',
          );
        },
      });
  }

  requestAction(action: UserAction): void {
    const user = this.selectedUser();
    if (
      !user ||
      (action === 'deactivate'
        ? !user.isActive || user.id === this.currentUser()?.id
        : user.isActive)
    )
      return;
    this.pendingAction.set(action);
    this.actionError.set(null);
  }

  confirmAction(): void {
    const user = this.selectedUser();
    const action = this.pendingAction();
    if (!user || !action || this.actionLoading()) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    const request =
      action === 'deactivate'
        ? this.usersService.deactivate(user.id)
        : this.usersService.reactivate(user.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.closeMenu();
        this.reload$.next();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoading.set(false);
        const detail = typeof error.error?.detail === 'string' ? error.error.detail : '';
        this.actionError.set(
          error.status === 409 && detail.includes('last active TenantAdmin')
            ? 'The last active Tenant Admin cannot be deactivated.'
            : error.status === 409 && detail.includes('own account')
              ? 'You cannot deactivate your own account.'
              : `Could not ${action} ${user.fullName}. Please try again.`,
        );
      },
    });
  }

  closeMenu(): void {
    if (this.actionLoading() || this.roleUpdateLoading()) return;
    this.pendingAction.set(null);
    this.selectedUser.set(null);
    this.actionError.set(null);
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    if (!this.pendingAction() && !this.showEditRolesModal()) this.closeMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeCreateModal();
    this.closeViewModal();
    if (this.showEditRolesModal()) this.closeEditRolesModal();
    else this.closeMenu();
  }
}
