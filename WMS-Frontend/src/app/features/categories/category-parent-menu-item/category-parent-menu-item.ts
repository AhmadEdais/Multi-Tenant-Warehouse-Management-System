import { Component, computed, forwardRef, input, output, signal } from '@angular/core';
import { CategoryNode } from '../models/category';

@Component({
  selector: 'app-category-parent-menu-item',
  imports: [forwardRef(() => CategoryParentMenuItem)],
  templateUrl: './category-parent-menu-item.html',
})
export class CategoryParentMenuItem {
  readonly node = input.required<CategoryNode>();
  readonly openLeft = input(false);
  readonly ancestors = input<readonly number[]>([]);
  readonly openPath = input<readonly number[]>([]);
  readonly openPathChange = output<number[]>();
  readonly choose = output<CategoryNode>();
  readonly expanded = computed(() => this.openPath()[this.ancestors().length] === this.node().id);
  readonly childAncestors = computed(() => [...this.ancestors(), this.node().id]);
  readonly submenuPosition = signal({ top: 0, left: 0 });

  toggleChildren(event: MouseEvent): void {
    event.stopPropagation();
    if (this.expanded()) {
      this.openPathChange.emit([...this.ancestors()]);
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.submenuPosition.set({
      top: Math.max(8, Math.min(rect.top, window.innerHeight - 270)),
      left: this.openLeft()
        ? Math.max(8, rect.left - 228)
        : Math.min(window.innerWidth - 232, rect.right + 4),
    });
    this.openPathChange.emit(this.childAncestors());
  }
}
