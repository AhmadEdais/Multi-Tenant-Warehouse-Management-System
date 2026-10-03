import { Component, input, OnInit, output } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CreateSupplierRequest, Supplier } from '../models/supplier';

const requiredText = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };

const optionalEmail = (control: AbstractControl): ValidationErrors | null => {
  const value = typeof control.value === 'string' ? control.value.trim() : '';
  return value ? Validators.email(new FormControl(value)) : null;
};

@Component({
  selector: 'app-supplier-form',
  imports: [ReactiveFormsModule],
  templateUrl: './supplier-form.html',
})
export class SupplierFormComponent implements OnInit {
  readonly mode = input.required<'create' | 'edit'>();
  readonly supplier = input<Supplier | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly close = output<void>();
  readonly save = output<CreateSupplierRequest>();

  readonly form = new FormGroup({
    code: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(50)],
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(200)],
    }),
    contactEmail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(256), optionalEmail],
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(50), Validators.pattern(/^\+?[0-9]+$/)],
    }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
  });

  ngOnInit(): void {
    const supplier = this.supplier();
    if (supplier) {
      this.form.reset({
        code: supplier.code,
        name: supplier.name,
        contactEmail: supplier.contactEmail ?? '',
        phoneNumber: supplier.phoneNumber,
        address: supplier.address ?? '',
      });
    }
  }

  submit(): void {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const values = this.form.getRawValue();
    this.save.emit({
      code: values.code.trim(),
      name: values.name.trim(),
      contactEmail: values.contactEmail.trim() || null,
      phoneNumber: values.phoneNumber.trim(),
      address: values.address.trim() || null,
    });
  }
}
