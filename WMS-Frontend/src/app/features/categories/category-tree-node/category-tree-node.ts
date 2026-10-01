import { Component, forwardRef, input, output } from '@angular/core';
import { CategoryNode } from '../models/category';

@Component({
  selector: 'app-category-tree-node',
  imports: [forwardRef(() => CategoryTreeNode)],
  templateUrl: './category-tree-node.html',
  styleUrl: './category-tree-node.css',
})
export class CategoryTreeNode {
  readonly node = input.required<CategoryNode>();
  readonly expandedIds = input.required<Set<number>>();
  readonly searchActive = input(false);
  readonly selectedId = input<number | null>(null);
  readonly canManage = input(false);

  readonly toggle = output<number>();
  readonly selectCategory = output<CategoryNode>();
  readonly openContextMenu = output<{ node: CategoryNode; x: number; y: number }>();

  isExpanded(): boolean {
    return this.searchActive() || this.expandedIds().has(this.node().id);
  }

  onToggle(event: MouseEvent): void {
    event.stopPropagation();
    if (!this.searchActive()) this.toggle.emit(this.node().id);
  }

  onContextMenu(event: MouseEvent): void {
    if (!this.canManage()) return;
    event.preventDefault();
    event.stopPropagation();
    this.openContextMenu.emit({ node: this.node(), x: event.clientX, y: event.clientY });
  }
}
