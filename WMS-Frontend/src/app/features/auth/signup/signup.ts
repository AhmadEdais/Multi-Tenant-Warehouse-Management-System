import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { SignupRequest } from '../models/signup-request';
import { AuthService } from '../auth.services';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { passwordsMatchValidator } from './signup-validator';
import { TextField } from '../../../shared/components/text-field/text-field';
import { PasswordField } from '../../../shared/components/password-field/password-field';

const combinedFullNameMaxLengthValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const firstName = control.get('firstName')?.value;
  const lastName = control.get('lastName')?.value;

  if (typeof firstName !== 'string' || typeof lastName !== 'string') {
    return null;
  }

  return `${firstName} ${lastName}`.length > 200 ? { fullNameMaxLength: true } : null;
};

@Component({
  imports: [ReactiveFormsModule, RouterLink, TextField, PasswordField],
  selector: 'app-signup',
  styleUrl: './signup.css',
  templateUrl: './signup.html',
})
export class Signup {
  private authService = inject(AuthService);
  private router = inject(Router);
  signupError = signal<string | null>(null);
  isSubmitting = signal(false);
  private handleSuccessfulSignup() {
    this.router.navigate(['/login']);
  }
  signupForm = new FormGroup(
    {
      firstName: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.minLength(3),
          Validators.pattern(/^[a-zA-Z\s]+$/),
        ],
      }),

      lastName: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.minLength(3),
          Validators.pattern(/^[a-zA-Z\s]+$/),
        ],
      }),

      email: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email],
      }),

      password: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(5)],
      }),

      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    { validators: [passwordsMatchValidator, combinedFullNameMaxLengthValidator] },
  );
  onSubmit() {
    this.signupError.set(null);

    if (this.signupForm.invalid) {
      this.signupForm.markAllAsTouched();
      return;
    }
    const { firstName, lastName, email, password } = this.signupForm.getRawValue();
    const fullName = firstName + ' ' + lastName;
    const request: SignupRequest = {
      fullName,
      email,
      password,
    };
    this.signupError.set(null);
    this.isSubmitting.set(true);
    this.authService
      .signup(request)
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.handleSuccessfulSignup();
        },
        error: (error) => {
          if (error.status === 409) {
            this.signupError.set('Email already exists. Please use a different email.');
          } else if (error.status === 400) {
            this.signupError.set(
              error.error?.detail ?? 'Please check the information you entered.',
            );
          } else {
            this.signupError.set('Something went wrong. Please try again.');
          }
        },
      });
  }
}
