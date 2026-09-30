import { Component, forwardRef, input, output } from '@angular/core';
import { LocationNode } from '../models/location';

@Component({
  selector: 'app-location-tree-node',
  imports: [forwardRef(() => LocationTreeNode)],
  templateUrl: './location-tree-node.html',
  styleUrl: './location-tree-node.css',
})
export class LocationTreeNode {
  readonly node = input.required<LocationNode>();
  readonly expandedIds = input.required<Set<number>>();
  readonly searchActive = input(false);

  readonly toggle = output<number>();

  isExpanded(): boolean {
    return this.searchActive() || this.expandedIds().has(this.node().id);
  }

  onToggle(event: MouseEvent): void {
    event.stopPropagation();
    if (!this.searchActive()) this.toggle.emit(this.node().id);
  }
}
