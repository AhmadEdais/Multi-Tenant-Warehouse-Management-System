import { Component, computed, ElementRef, input, output, signal, ViewChild } from '@angular/core';
import { LocationNode } from '../../locations/models/location';

export interface SelectedBin {
  id: number;
  path: string;
}

interface PickerRow {
  id: number;
  name: string;
  path: string;
  depth: number;
  isBin: boolean;
  parentIds: number[];
}

@Component({
  selector: 'app-bin-picker',
  templateUrl: './bin-picker.html',
})
export class BinPickerComponent {
  readonly tree = input.required<LocationNode[]>();
  readonly excludedIds = input.required<number[]>();
  readonly selected = output<SelectedBin>();
  readonly close = output<void>();
  readonly search = signal('');
  readonly collapsedIds = signal<ReadonlySet<number>>(new Set());

  @ViewChild('searchField')
  set searchField(field: ElementRef<HTMLInputElement> | undefined) {
    field?.nativeElement.focus();
  }

  readonly rows = computed(() => {
    const result: PickerRow[] = [];
    const visit = (nodes: LocationNode[], names: string[], parentIds: number[]): void => {
      for (const node of nodes) {
        if (node.locationType === 'Dock') continue;
        const path = [...names, node.name];
        if (node.locationType === 'Bin') {
          if (node.isActive) result.push({ id: node.id, name: node.name, path: path.join(' / '), depth: parentIds.length, isBin: true, parentIds });
        } else if (node.locationType === 'Zone' || node.locationType === 'Aisle' || node.locationType === 'Rack') {
          result.push({ id: node.id, name: node.name, path: path.join(' / '), depth: parentIds.length, isBin: false, parentIds });
          visit(node.children ?? [], path, [...parentIds, node.id]);
        }
      }
    };
    visit(this.tree(), [], []);
    return result;
  });

  readonly visibleRows = computed(() => {
    const term = this.search().trim().toLocaleLowerCase();
    const rows = this.rows();
    if (term) {
      const matchingBins = rows.filter((row) => row.isBin && row.path.toLocaleLowerCase().includes(term));
      const visibleIds = new Set(matchingBins.flatMap((row) => [...row.parentIds, row.id]));
      return rows.filter((row) => visibleIds.has(row.id));
    }
    return rows.filter((row) => row.parentIds.every((id) => !this.collapsedIds().has(id)));
  });

  toggle(id: number): void {
    this.collapsedIds.update((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  choose(row: PickerRow): void {
    if (row.isBin && !this.excludedIds().includes(row.id)) {
      this.selected.emit({ id: row.id, path: row.path });
    }
  }
}
