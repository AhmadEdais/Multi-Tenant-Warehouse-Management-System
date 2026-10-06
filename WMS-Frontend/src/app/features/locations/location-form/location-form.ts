import { Component, input, OnInit, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Warehouse } from '../../warehouses/models/warehouse';
import {
  ADDABLE_LOCATION_TYPES,
  AddableLocationType,
  isAddableLocationType,
  isEditableLocationType,
  LocationNode,
} from '../models/location';

export interface LocationFormValue {
  locationType: AddableLocationType;
  parentLocationId: number | null;
  name: string;
  barcode: string | null;
  maxWeightCapacityKg: number | null;
}

const parentType: Record<AddableLocationType, AddableLocationType | null> = {
  Zone: null,
  Aisle: 'Zone',
  Rack: 'Aisle',
  Bin: 'Rack',
};

@Component({
  selector: 'app-location-form',
  imports: [ReactiveFormsModule],
  templateUrl: './location-form.html',
})
export class LocationFormComponent implements OnInit {
  readonly mode = input.required<'add' | 'edit'>();
  readonly warehouse = input.required<Warehouse>();
  readonly locationTree = input.required<LocationNode[]>();
  readonly location = input<LocationNode | null>(null);
  readonly lockedParent = input<LocationNode | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly close = output<void>();
  readonly submitLocation = output<LocationFormValue>();

  readonly types = ADDABLE_LOCATION_TYPES;
  readonly form = new FormGroup({
    locationType: new FormControl<AddableLocationType>('Zone', { nonNullable: true, validators: Validators.required }),
    parentLocationId: new FormControl<number | null>(null),
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    barcode: new FormControl('', { nonNullable: true, validators: Validators.maxLength(100) }),
    maxWeightCapacityKg: new FormControl<number | null>(null, Validators.min(0)),
  });

  ngOnInit(): void {
    const location = this.location();
    const lockedParent = this.lockedParent();
    const type =
      location && isEditableLocationType(location.locationType)
        ? location.locationType
        : this.childType(lockedParent) ?? ADDABLE_LOCATION_TYPES[0];
    this.form.reset({
      locationType: type,
      parentLocationId: location?.parentLocationId ?? lockedParent?.id ?? null,
      name: location?.name ?? '',
      barcode: location?.barcode ?? '',
      maxWeightCapacityKg: location?.maxWeightCapacityKg ?? null,
    });
    if (location || lockedParent) this.form.controls.locationType.disable();
    this.updateParentControl();
  }

  get parentOptions(): LocationNode[] {
    const requiredType = parentType[this.form.controls.locationType.value];
    if (!requiredType) return [];
    const results: LocationNode[] = [];
    const visit = (nodes: LocationNode[]): void => {
      for (const node of nodes) {
        if (node.isActive && node.locationType === requiredType && node.id !== this.location()?.id) {
          results.push(node);
        }
        visit(node.children);
      }
    };
    visit(this.locationTree());
    return results;
  }

  onTypeChange(event: Event): void {
    const type = (event.target as HTMLSelectElement).value;
    if (!isAddableLocationType(type)) return;
    this.form.controls.locationType.setValue(type);
    this.form.controls.parentLocationId.setValue(null);
    this.updateParentControl();
  }

  submit(): void {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const name = value.name.trim();
    if (!name) {
      this.form.controls.name.setErrors({ required: true });
      this.form.controls.name.markAsTouched();
      return;
    }
    this.submitLocation.emit({
      locationType: value.locationType,
      parentLocationId: value.parentLocationId,
      name,
      barcode: value.barcode.trim() || null,
      maxWeightCapacityKg: value.maxWeightCapacityKg,
    });
  }

  private childType(parent: LocationNode | null): AddableLocationType | null {
    if (!parent) return null;
    switch (parent.locationType) {
      case 'Zone': return 'Aisle';
      case 'Aisle': return 'Rack';
      case 'Rack': return 'Bin';
      default: return null;
    }
  }

  private updateParentControl(): void {
    const parent = this.form.controls.parentLocationId;
    if (this.form.controls.locationType.value === 'Zone' || this.lockedParent()) {
      parent.clearValidators();
      parent.disable();
    } else {
      parent.enable();
      parent.setValidators(Validators.required);
    }
    parent.updateValueAndValidity();
  }
}
