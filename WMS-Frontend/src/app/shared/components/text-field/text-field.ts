import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-text-field',
  imports: [ReactiveFormsModule],
  templateUrl: './text-field.html',
})
export class TextField {
  label = input.required<string>();
  id = input.required<string>();
  control = input.required<FormControl<string>>();
  type = input<'text' | 'email'>('text');
  placeholder = input.required<string>();
  autocomplete = input.required<string>();
}
