import { Component, input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-password-field',
  imports: [ReactiveFormsModule],
  templateUrl: './password-field.html',
})
export class PasswordField {
  control = input.required<FormControl<string>>();

  label = input.required<string>();
  inputId = input.required<string>();
  placeholder = input.required<string>();

  autocomplete = input<'current-password' | 'new-password'>('current-password');

  visible = signal(false);

  toggleVisibility(): void {
    this.visible.update((value) => !value);
  }
}
