import { EMPTY, expand, map, Observable, reduce } from 'rxjs';
import { Supplier } from '../../suppliers/models/supplier';
import { SuppliersService } from '../../suppliers/suppliers.service';
import { Warehouse } from '../../warehouses/models/warehouse';
import { WarehousesService } from '../../warehouses/warehouses.service';

export interface PurchaseOrderFormOption {
  id: number;
  label: string;
}

export function loadActiveSupplierOptions(
  service: SuppliersService,
): Observable<PurchaseOrderFormOption[]> {
  return service.list({ searchTerm: '', isActive: true, pageNumber: 1, pageSize: 100 }).pipe(
    expand((page) =>
      page.pageNumber * page.pageSize < page.totalCount
        ? service.list({
            searchTerm: '',
            isActive: true,
            pageNumber: page.pageNumber + 1,
            pageSize: 100,
          })
        : EMPTY,
    ),
    reduce((items, page) => items.concat(page.data), [] as Supplier[]),
    map((items) =>
      items
        .filter((item) => item.isActive)
        .map((item) => ({ id: item.id, label: `${item.code} — ${item.name}` })),
    ),
  );
}

export function loadActiveWarehouseOptions(
  service: WarehousesService,
): Observable<PurchaseOrderFormOption[]> {
  return service.list({ search: '', isActive: true, pageNumber: 1, pageSize: 100 }).pipe(
    expand((page) =>
      page.pageNumber * page.pageSize < page.totalCount
        ? service.list({
            search: '',
            isActive: true,
            pageNumber: page.pageNumber + 1,
            pageSize: 100,
          })
        : EMPTY,
    ),
    reduce((items, page) => items.concat(page.data), [] as Warehouse[]),
    map((items) =>
      items
        .filter((item) => item.isActive)
        .map((item) => ({ id: item.id, label: `${item.code} — ${item.name}` })),
    ),
  );
}
