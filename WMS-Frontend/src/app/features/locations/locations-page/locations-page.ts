import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, HostListener, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, expand, Observable, of, reduce, Subject, switchMap } from 'rxjs';
import { Warehouse } from '../../warehouses/models/warehouse';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { LocationTreeNode } from '../location-node/location-tree-node';
import { LocationFormComponent, LocationFormValue } from '../location-form/location-form';
import { LocationsService } from '../locations.service';
import { CreateLocationRequest, LocationDetails, LocationNode, UpdateLocationRequest } from '../models/location';

type LocationStatus = 'all' | 'active' | 'inactive';
type FormState = { mode: 'add' | 'edit'; location: LocationNode | null; lockedParent: LocationNode | null };
type ActionState = { action: 'deactivate' | 'reactivate'; location: LocationNode };

@Component({
  selector: 'app-locations-page',
  imports: [DecimalPipe, LocationTreeNode, LocationFormComponent],
  templateUrl: './locations-page.html',
  styleUrl: './locations-page.css',
})
export class LocationsPage implements OnInit {
  private readonly locationsService = inject(LocationsService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly treeLoad$ = new Subject<number>();
  private readonly detailsSelection$ = new Subject<LocationNode | null>();

  readonly warehouses = signal<Warehouse[]>([]);
  readonly selectedWarehouse = signal<Warehouse | null>(null);
  readonly warehousesLoading = signal(false);
  readonly warehousesError = signal<string | null>(null);
  readonly locationTree = signal<LocationNode[]>([]);
  readonly treeLoading = signal(false);
  readonly treeError = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly status = signal<LocationStatus>('all');
  readonly expandedIds = signal<Set<number>>(new Set());
  readonly warehouseRootExpanded = signal(true);
  readonly selectedLocationNode = signal<LocationNode | null>(null);
  readonly selectedLocationDetails = signal<LocationDetails | null>(null);
  readonly detailsLoading = signal(false);
  readonly detailsError = signal<string | null>(null);
  readonly contextLocation = signal<LocationNode | null>(null);
  readonly contextPosition = signal({ top: 0, left: 0 });
  readonly formState = signal<FormState | null>(null);
  readonly formLoading = signal(false);
  readonly formError = signal<string | null>(null);
  readonly pendingAction = signal<ActionState | null>(null);
  readonly actionLoading = signal(false);
  readonly actionError = signal<string | null>(null);

  readonly isSearching = computed(() => this.searchTerm().trim().length > 0);
  readonly isFiltering = computed(() => this.isSearching() || this.status() !== 'all');
  readonly filteredTree = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const status = this.status();
    return term || status !== 'all'
      ? this.filterLocations(this.locationTree(), term, status)
      : this.locationTree();
  });

  ngOnInit(): void {
    this.treeLoad$
      .pipe(
        switchMap((warehouseId) => {
          this.treeLoading.set(true);
          this.treeError.set(null);
          return this.locationsService.getTree(warehouseId).pipe(
            catchError(() => {
              this.treeError.set('Could not load locations. Please try again.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((tree) => {
        this.treeLoading.set(false);
        this.locationTree.set(tree ?? []);
      });

    this.detailsSelection$
      .pipe(
        switchMap((node) => {
          this.selectedLocationNode.set(node);
          this.selectedLocationDetails.set(null);
          this.detailsError.set(null);
          this.detailsLoading.set(node !== null);
          if (!node) return EMPTY;

          return this.locationsService.getById(node.id).pipe(
            catchError(() => {
              this.detailsLoading.set(false);
              this.detailsError.set('Could not load location details. Please try again.');
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((details) => {
        this.detailsLoading.set(false);
        this.selectedLocationDetails.set(details);
        this.selectedLocationNode.update((node) =>
          node?.id === details.id
            ? {
                ...node,
                parentLocationId: details.parentLocationId,
                locationType: details.locationType,
                name: details.name,
                barcode: details.barcode,
                maxWeightCapacityKg: details.maxWeightCapacityKg,
                isActive: details.isActive,
              }
            : node,
        );
      });

    this.loadWarehouses();
  }

  loadWarehouses(): void {
    if (this.warehousesLoading()) return;
    this.warehousesLoading.set(true);
    this.warehousesError.set(null);

    const pageSize = 100;
    const params = { pageNumber: 1, pageSize, search: '', isActive: true };
    this.warehousesService
      .list(params)
      .pipe(
        expand((page) =>
          page.pageNumber * page.pageSize < page.totalCount
            ? this.warehousesService.list({ ...params, pageNumber: page.pageNumber + 1 })
            : EMPTY,
        ),
        reduce((all, page) => all.concat(page.data), [] as Warehouse[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (warehouses) => {
          this.warehousesLoading.set(false);
          this.warehouses.set(warehouses);
          if (warehouses.length) this.selectWarehouse(warehouses[0]);
        },
        error: () => {
          this.warehousesLoading.set(false);
          this.warehousesError.set('Could not load warehouses. Please try again.');
        },
      });
  }

  onWarehouseChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value);
    const warehouse = this.warehouses().find((item) => item.id === id);
    if (warehouse && warehouse.id !== this.selectedWarehouse()?.id) {
      this.selectWarehouse(warehouse);
    }
  }

  private selectWarehouse(warehouse: Warehouse): void {
    this.closeContextMenu();
    this.formState.set(null);
    this.pendingAction.set(null);
    this.detailsSelection$.next(null);
    this.selectedWarehouse.set(warehouse);
    this.locationTree.set([]);
    this.searchTerm.set('');
    this.expandedIds.set(new Set());
    this.warehouseRootExpanded.set(true);
    this.treeLoad$.next(warehouse.id);
  }

  retryTree(): void {
    const warehouse = this.selectedWarehouse();
    if (warehouse) this.treeLoad$.next(warehouse.id);
  }

  onLocationSelected(node: LocationNode): void {
    this.detailsSelection$.next(node);
  }

  retryDetails(): void {
    const node = this.selectedLocationNode();
    if (node) this.detailsSelection$.next(node);
  }

  onSearchInput(event: Event): void {
    this.closeContextMenu();
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  onStatusChange(event: Event): void {
    this.closeContextMenu();
    this.status.set((event.target as HTMLSelectElement).value as LocationStatus);
  }

  toggleWarehouseRoot(): void {
    if (!this.isFiltering()) this.warehouseRootExpanded.update((expanded) => !expanded);
  }

  toggleExpanded(locationId: number): void {
    if (this.isFiltering()) return;
    this.expandedIds.update((current) => {
      const updated = new Set(current);
      if (updated.has(locationId)) updated.delete(locationId);
      else updated.add(locationId);
      return updated;
    });
  }

  private filterLocations(locations: LocationNode[], term: string, status: LocationStatus): LocationNode[] {
    return locations
      .map((location) => {
        const filteredChildren = this.filterLocations(location.children, term, status);
        const matchesSearch = !term || location.name.toLowerCase().includes(term) ||
          !!location.barcode?.toLowerCase().includes(term);
        const matchesStatus = status === 'all' || location.isActive === (status === 'active');

        return (matchesSearch && matchesStatus) || filteredChildren.length > 0
          ? { ...location, children: filteredChildren }
          : null;
      })
      .filter((location): location is LocationNode => location !== null);
  }

  openLocationContext(event: { node: LocationNode; x: number; y: number }): void {
    this.contextLocation.set(event.node);
    this.contextPosition.set({
      top: Math.max(8, Math.min(event.y, window.innerHeight - 150)),
      left: Math.max(8, Math.min(event.x, window.innerWidth - 180)),
    });
  }

  closeContextMenu(): void {
    this.contextLocation.set(null);
  }

  openAddLocation(): void {
    if (!this.selectedWarehouse() || this.treeLoading() || this.treeError()) return;
    this.closeContextMenu();
    this.formError.set(null);
    this.formState.set({ mode: 'add', location: null, lockedParent: null });
  }

  openAddChild(): void {
    const parent = this.contextLocation();
    if (!parent?.isActive || parent.locationType === 'Bin') return;
    this.formError.set(null);
    this.formState.set({ mode: 'add', location: null, lockedParent: parent });
    this.closeContextMenu();
  }

  openEditLocation(location: LocationNode | null = this.contextLocation()): void {
    if (!location?.isActive) return;
    this.formError.set(null);
    this.formState.set({ mode: 'edit', location, lockedParent: null });
    this.closeContextMenu();
  }

  closeLocationForm(): void {
    if (this.formLoading()) return;
    this.formState.set(null);
    this.formError.set(null);
  }

  saveLocation(value: LocationFormValue): void {
    const state = this.formState();
    const warehouse = this.selectedWarehouse();
    if (!state || !warehouse || this.formLoading()) return;
    this.formLoading.set(true);
    this.formError.set(null);
    const request: Observable<unknown> = state.mode === 'add'
      ? this.locationsService.create({ warehouseId: warehouse.id, ...value } satisfies CreateLocationRequest)
      : this.locationsService.update(state.location!.id, {
          parentLocationId: value.parentLocationId,
          name: value.name,
          barcode: value.barcode,
          maxWeightCapacityKg: value.maxWeightCapacityKg,
        } satisfies UpdateLocationRequest);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.formLoading.set(false);
        this.closeLocationForm();
        this.treeLoad$.next(warehouse.id);
        if (this.selectedLocationNode()) this.retryDetails();
      },
      error: (error: HttpErrorResponse) => {
        this.formLoading.set(false);
        this.formError.set(this.locationError(error, 'Could not save location. Please try again.'));
      },
    });
  }

  requestAction(action: 'deactivate' | 'reactivate', location: LocationNode | null = this.contextLocation()): void {
    if (!location || location.isActive !== (action === 'deactivate')) return;
    this.pendingAction.set({ action, location });
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
    const warehouse = this.selectedWarehouse();
    if (!pending || !warehouse || this.actionLoading()) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    const request = pending.action === 'deactivate'
      ? this.locationsService.deactivate(pending.location.id)
      : this.locationsService.reactivate(pending.location.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.closeAction();
        this.treeLoad$.next(warehouse.id);
        if (this.selectedLocationNode()) this.retryDetails();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoading.set(false);
        const message = this.locationError(error, `Could not ${pending.action} location. Please try again.`);
        this.actionError.set(pending.action === 'reactivate' && message === 'The selected parent is inactive.'
          ? 'This location cannot be reactivated until its parent is active.' : message);
      },
    });
  }

  private locationError(error: HttpErrorResponse, fallback: string): string {
    const detail = typeof error.error?.detail === 'string' ? error.error.detail : '';
    const known = [
      'Barcode already exists in this warehouse.',
      'The selected parent is inactive.',
      'This location has active child locations and cannot be deactivated.',
      'This bin still contains stock and cannot be deactivated.',
      'Parent location must belong to the same warehouse.',
      'A Zone cannot have a parent location.',
      'Cannot add a location to an inactive warehouse.',
      'Inactive locations cannot be edited.',
    ];
    if (error.status === 409 && (known.includes(detail) || /^A (Aisle|Rack|Bin) must belong to a (Zone|Aisle|Rack)\.$/.test(detail))) return detail;
    if (error.status === 404) return 'This location or its parent is no longer available. Refresh and try again.';
    if (error.status === 400) return 'Please check the location information and try again.';
    return fallback;
  }

  @HostListener('document:click')
  onOutsideClick(): void { this.closeContextMenu(); }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeContextMenu();
    this.closeLocationForm();
    this.closeAction();
  }
}
