import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, expand, of, reduce, Subject, switchMap } from 'rxjs';
import { Warehouse } from '../../warehouses/models/warehouse';
import { WarehousesService } from '../../warehouses/warehouses.service';
import { LocationTreeNode } from '../location-node/location-tree-node';
import { LocationsService } from '../locations.service';
import { LocationNode } from '../models/location';

@Component({
  selector: 'app-locations-page',
  imports: [LocationTreeNode],
  templateUrl: './locations-page.html',
  styleUrl: './locations-page.css',
})
export class LocationsPage implements OnInit {
  private readonly locationsService = inject(LocationsService);
  private readonly warehousesService = inject(WarehousesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly treeLoad$ = new Subject<number>();

  readonly warehouses = signal<Warehouse[]>([]);
  readonly selectedWarehouse = signal<Warehouse | null>(null);
  readonly warehousesLoading = signal(false);
  readonly warehousesError = signal<string | null>(null);
  readonly locationTree = signal<LocationNode[]>([]);
  readonly treeLoading = signal(false);
  readonly treeError = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly expandedIds = signal<Set<number>>(new Set());
  readonly warehouseRootExpanded = signal(true);

  readonly isSearching = computed(() => this.searchTerm().trim().length > 0);
  readonly filteredTree = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    return term ? this.filterLocations(this.locationTree(), term) : this.locationTree();
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

  onSearchInput(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  toggleWarehouseRoot(): void {
    if (!this.isSearching()) this.warehouseRootExpanded.update((expanded) => !expanded);
  }

  toggleExpanded(locationId: number): void {
    if (this.isSearching()) return;
    this.expandedIds.update((current) => {
      const updated = new Set(current);
      if (updated.has(locationId)) updated.delete(locationId);
      else updated.add(locationId);
      return updated;
    });
  }

  private filterLocations(locations: LocationNode[], term: string): LocationNode[] {
    return locations
      .map((location) => {
        const filteredChildren = this.filterLocations(location.children, term);
        const matches =
          location.name.toLowerCase().includes(term) ||
          location.barcode?.toLowerCase().includes(term);

        return matches || filteredChildren.length > 0
          ? { ...location, children: filteredChildren }
          : null;
      })
      .filter((location): location is LocationNode => location !== null);
  }
}
