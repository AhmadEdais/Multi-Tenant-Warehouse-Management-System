import { EMPTY, expand, map, Observable, reduce } from 'rxjs';
import { Customer } from '../../customers/models/customer';
import { CustomersService } from '../../customers/customers.service';
import type { SalesOrderFormOption } from './sales-order-form';

export function loadActiveCustomerOptions(
  service: CustomersService,
): Observable<SalesOrderFormOption[]> {
  return service.list({ searchTerm: '', isActive: true, pageNumber: 1, pageSize: 100 }).pipe(
    expand((page) => page.pageNumber * page.pageSize < page.totalCount
      ? service.list({ searchTerm: '', isActive: true, pageNumber: page.pageNumber + 1, pageSize: 100 })
      : EMPTY),
    reduce((items, page) => items.concat(page.data), [] as Customer[]),
    map((items) => items.filter((item) => item.isActive)
      .map((item) => ({ id: item.id, label: `${item.code} — ${item.name}` }))),
  );
}
