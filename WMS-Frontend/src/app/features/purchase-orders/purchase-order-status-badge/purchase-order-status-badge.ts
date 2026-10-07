import { Component, computed, input } from '@angular/core';
import { PurchaseOrderStatus } from '../models/purchase-order';

@Component({
  selector: 'app-purchase-order-status-badge',
  template: `<span
    class="inline-flex rounded-md px-2.5 py-1 text-xs font-semibold"
    [class]="badgeClass()"
    >{{ status() }}</span
  >`,
})
export class PurchaseOrderStatusBadge {
  readonly status = input.required<PurchaseOrderStatus>();
  readonly badgeClass = computed(() => {
    switch (this.status()) {
      case 'Draft':
        return 'bg-[#eef2f7] text-[#52647e]';
      case 'Pending':
        return 'bg-[#e7efff] text-[#2458c5]';
      case 'Receiving':
        return 'bg-[#fff2db] text-[#9a5a08]';
      case 'Received':
        return 'bg-[#e1f7eb] text-[#116b48]';
      case 'Canceled':
        return 'bg-[#ffe9e9] text-[#bd2d31]';
    }
  });
}
