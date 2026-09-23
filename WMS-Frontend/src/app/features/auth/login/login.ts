import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, SessionInitializationError } from '../auth.services';
import { LoginRequest } from '../models/login-request';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { TextField } from '../../../shared/components/text-field/text-field';
import { PasswordField } from '../../../shared/components/password-field/password-field';

@Component({
  imports: [ReactiveFormsModule, TextField, PasswordField],
  selector: 'app-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);
  loginError = signal<string | null>(null);
  isSubmitting = signal(false);

  loginForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),

    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    }),
    rememberMe: new FormControl(false, { nonNullable: true }),
  });

  onSubmit() {
    this.loginError.set(null);
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    const { email, password } = this.loginForm.getRawValue();
    const request: LoginRequest = {
      email,
      password,
    };
    const rememberMe = this.loginForm.controls.rememberMe.value;
    this.isSubmitting.set(true);

    this.authService
      .login(request, rememberMe)
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: () => {
          this.router.navigate(['/dashboard']);
        },

        error: (error) => {
          if (error instanceof SessionInitializationError) {
            this.loginError.set('Could not load your account. Please sign in again.');
          } else if (error.status === 401) {
            this.loginError.set('Invalid email or password.');
          } else {
            this.loginError.set('Something went wrong. Please try again.');
          }
        },
      });
  }
}
