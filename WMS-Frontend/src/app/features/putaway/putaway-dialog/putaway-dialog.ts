import { DecimalPipe } from '@angular/common';
import { Component, computed, DestroyRef, ElementRef, HostListener, inject, input, OnInit, output, signal, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocationsService } from '../../locations/locations.service';
import { LocationNode } from '../../locations/models/location';
import { BinPickerComponent, SelectedBin } from '../bin-picker/bin-picker';
import { PutawayQueueItem, PutawayRequest } from '../models/putaway';

type DestinationForm = FormGroup<{
  locationId: FormControl<number | null>;
  quantity: FormControl<number | null>;
}>;

@Component({
  selector: 'app-putaway-dialog',
  imports: [DecimalPipe, ReactiveFormsModule, BinPickerComponent],
  templateUrl: './putaway-dialog.html',
})
export class PutawayDialogComponent implements OnInit {
  private readonly locationsService = inject(LocationsService);
  private readonly destroyRef = inject(DestroyRef);
  private pickerTrigger: HTMLElement | null = null;

  readonly warehouseId = input.required<number>();
  readonly warehouseName = input.required<string>();
  readonly item = input.required<PutawayQueueItem>();
  readonly submitting = input(false);
  readonly serverError = input<string | null>(null);
  readonly close = output<void>();
  readonly submitPutaway = output<PutawayRequest>();

  readonly destinations = new FormArray<DestinationForm>([this.newDestination()]);
  readonly selectedPaths = signal<string[]>(['']);
  readonly tree = signal<LocationNode[]>([]);
  readonly treeLoading = signal(true);
  readonly treeError = signal<string | null>(null);
  readonly pickerIndex = signal<number | null>(null);
  private readonly formRevision = signal(0);

  @ViewChild('dialogTitle')
  set dialogTitle(title: ElementRef<HTMLElement> | undefined) {
    title?.nativeElement.focus();
  }

  readonly selectedIds = computed(() => {
    this.formRevision();
    return this.destinations.controls
      .map((group) => group.controls.locationId.value)
      .filter((id): id is number => id !== null);
  });
  readonly movingCents = computed(() => {
    this.formRevision();
    return this.destinations.controls.reduce((sum, group) => {
      const quantity = group.controls.quantity.value;
      return sum + (typeof quantity === 'number' && Number.isFinite(quantity) && quantity > 0
        ? Math.round(quantity * 100) : 0);
    }, 0);
  });
  readonly moving = computed(() => this.movingCents() / 100);
  readonly remaining = computed(() => Math.max(0, Math.round(this.item().quantityOnHand * 100) - this.movingCents()) / 100);
  readonly overAvailable = computed(() => this.movingCents() > Math.round(this.item().quantityOnHand * 100));
  readonly duplicateBins = computed(() => {
    const ids = this.selectedIds();
    return ids.length !== new Set(ids).size;
  });
  readonly canSubmit = computed(() => {
    this.formRevision();
    return !this.submitting() && this.destinations.valid && !this.duplicateBins()
      && !this.overAvailable() && this.movingCents() > 0;
  });

  ngOnInit(): void {
    this.destinations.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.formRevision.update((value) => value + 1));
    this.loadTree();
  }

  loadTree(): void {
    this.treeLoading.set(true);
    this.treeError.set(null);
    this.locationsService.getTree(this.warehouseId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (tree) => {
        this.tree.set(tree);
        this.treeLoading.set(false);
      },
      error: () => {
        this.treeLoading.set(false);
        this.treeError.set('Could not load warehouse Bins. Please try again.');
      },
    });
  }

  addDestination(): void {
    if (this.submitting()) return;
    this.destinations.push(this.newDestination());
    this.selectedPaths.update((paths) => [...paths, '']);
  }

  removeDestination(index: number): void {
    if (this.submitting() || this.destinations.length <= 1) return;
    this.destinations.removeAt(index);
    this.selectedPaths.update((paths) => paths.filter((_, row) => row !== index));
  }

  openPicker(index: number, event: Event): void {
    if (this.submitting() || this.treeLoading() || this.treeError()) return;
    this.pickerTrigger = event.currentTarget as HTMLElement;
    this.pickerIndex.set(index);
  }

  selectBin(bin: SelectedBin): void {
    const index = this.pickerIndex();
    if (index === null) return;
    this.destinations.at(index).controls.locationId.setValue(bin.id);
    this.selectedPaths.update((paths) => paths.map((path, row) => row === index ? bin.path : path));
    this.closePicker();
  }

  closePicker(): void {
    this.pickerIndex.set(null);
    queueMicrotask(() => this.pickerTrigger?.focus());
  }

  requestClose(): void {
    if (this.submitting()) return;
    if (this.pickerIndex() !== null) this.closePicker();
    else this.close.emit();
  }

  confirm(): void {
    if (this.submitting()) return;
    this.destinations.markAllAsTouched();
    if (!this.canSubmit()) return;
    this.submitPutaway.emit({
      warehouseId: this.warehouseId(),
      productId: this.item().productId,
      destinations: this.destinations.controls.map((group) => ({
        locationId: group.controls.locationId.value!,
        quantity: group.controls.quantity.value!,
      })),
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.requestClose();
  }

  private newDestination(): DestinationForm {
    return new FormGroup({
      locationId: new FormControl<number | null>(null, Validators.required),
      quantity: new FormControl<number | null>(null, [Validators.required, (control) => {
        const value = control.value as number | null;
        if (value === null) return null;
        if (typeof value !== 'number' || !Number.isFinite(value) || value <= 0) return { positive: true };
        return /^\d{1,16}(?:\.\d{1,2})?$/.test(String(value)) ? null : { precision: true };
      }]),
    });
  }
}
