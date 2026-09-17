import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { SignupRequest } from '../models/signup-request';
import { AuthService } from '../auth.services';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { passwordsMatchValidator } from './signup-validator';
@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-signup',
  styleUrl: './signup.css',
  templateUrl: './signup.html',
})
export class Signup {
  private authService = inject(AuthService);
  private router = inject(Router);
  signupError = signal<string | null>(null);
  isSubmitting = signal(false);
  passwordVisible = signal(false);
  private handleSuccessfulSignup() {
    this.router.navigate(['/login']);
  }
  signupForm = new FormGroup(
    {
      fullName: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.maxLength(200),
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
    { validators: passwordsMatchValidator },
  );
  onSubmit() {
    this.signupError.set(null);

    if (this.signupForm.invalid) {
      this.signupForm.markAllAsTouched();
      return;
    }
    const { fullName, email, password } = this.signupForm.getRawValue();
    const request: SignupRequest = {
      fullName,
      email,
      password,
    };
    console.log('Signup request:', request);
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
          if (error.status === 400 && error.error?.message) {
            this.signupError.set(error.error.message);
          } else if (error.status === 409) {
            this.signupError.set('Email already exists. Please use a different email.');
          } else {
            this.signupError.set('An error occurred during signup.');
          }
        },
      });
  }
  togglePasswordVisibility() {
    this.passwordVisible.set(!this.passwordVisible());
  }
}
