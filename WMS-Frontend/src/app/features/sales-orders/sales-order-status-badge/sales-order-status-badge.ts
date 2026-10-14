import { Component, computed, input } from '@angular/core';
import { SalesOrderStatus } from '../models/sales-order';

@Component({
  selector: 'app-sales-order-status-badge',
  template: `<span class="inline-flex rounded-md px-2.5 py-1 text-xs font-semibold" [class]="badgeClass()">{{ status() }}</span>`,
})
export class SalesOrderStatusBadge {
  readonly status = input.required<SalesOrderStatus>();
  readonly badgeClass = computed(() => {
    switch (this.status()) {
      case 'Draft': return 'bg-[#eef2f7] text-[#52647e]';
      case 'Allocated': return 'bg-[#e7efff] text-[#2458c5]';
      case 'Picked': return 'bg-[#fff2db] text-[#9a5a08]';
      case 'Shipped': return 'bg-[#e1f7eb] text-[#116b48]';
      case 'Canceled': return 'bg-[#ffe9e9] text-[#bd2d31]';
    }
  });
}
