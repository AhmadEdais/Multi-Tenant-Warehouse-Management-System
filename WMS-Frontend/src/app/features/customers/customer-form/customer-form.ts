import { Component, input, OnInit, output } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CreateCustomerRequest, Customer } from '../models/customer';

const requiredText = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };

const optionalEmail = (control: AbstractControl): ValidationErrors | null => {
  const value = typeof control.value === 'string' ? control.value.trim() : '';
  return value ? Validators.email(new FormControl(value)) : null;
};

const creditLimitAmount = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value as number | null;
  if (value === null) return null;
  return Number.isFinite(value) && /^\d{1,16}(?:\.\d{1,2})?$/.test(String(value))
    ? null
    : { creditLimit: true };
};

@Component({
  selector: 'app-customer-form',
  imports: [ReactiveFormsModule],
  templateUrl: './customer-form.html',
})
export class CustomerFormComponent implements OnInit {
  readonly mode = input.required<'create' | 'edit'>();
  readonly customer = input<Customer | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly close = output<void>();
  readonly save = output<CreateCustomerRequest>();

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
    creditLimit: new FormControl<number | null>(null, { validators: [creditLimitAmount] }),
  });

  ngOnInit(): void {
    const customer = this.customer();
    if (customer) {
      this.form.reset({
        code: customer.code,
        name: customer.name,
        contactEmail: customer.contactEmail ?? '',
        phoneNumber: customer.phoneNumber,
        address: customer.address ?? '',
        creditLimit: customer.creditLimit,
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
      creditLimit: values.creditLimit,
    });
  }
}
