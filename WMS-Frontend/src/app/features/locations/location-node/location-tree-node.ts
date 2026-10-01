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
  readonly selectedId = input<number | null>(null);

  readonly toggle = output<number>();
  readonly selectLocation = output<LocationNode>();
  readonly openContextMenu = output<{ node: LocationNode; x: number; y: number }>();

  isSelected(): boolean {
    return this.selectedId() === this.node().id;
  }

  isExpanded(): boolean {
    return this.searchActive() || this.expandedIds().has(this.node().id);
  }

  onToggle(event: MouseEvent): void {
    event.stopPropagation();
    if (!this.searchActive()) this.toggle.emit(this.node().id);
  }

  onSelect(): void {
    this.selectLocation.emit(this.node());
  }

  onContextMenu(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.openContextMenu.emit({ node: this.node(), x: event.clientX, y: event.clientY });
  }
}
