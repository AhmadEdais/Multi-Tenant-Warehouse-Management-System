import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, landingRouteFor, SessionInitializationError } from '../auth.services';
import { LoginRequest } from '../models/login-request';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { TextField } from '../../../shared/components/text-field/text-field';
import { PasswordField } from '../../../shared/components/password-field/password-field';
import { DemoLaunch } from '../demo-launch/demo-launch';

@Component({
  imports: [ReactiveFormsModule, TextField, PasswordField, DemoLaunch],
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
    if (this.isSubmitting()) return;
    this.loginError.set(null);
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    const { email, password, rememberMe } = this.loginForm.getRawValue();
    const request: LoginRequest = {
      email,
      password,
      rememberMe,
    };
    this.isSubmitting.set(true);

    this.authService
      .login(request)
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: (user) => {
          void this.router.navigateByUrl(landingRouteFor(user));
        },

        error: (error) => {
          if (error instanceof SessionInitializationError) {
            if (error.cause instanceof HttpErrorResponse && error.cause.status === 401) {
              this.loginError.set('Could not load your account. Please sign in again.');
            } else if (error.cause instanceof HttpErrorResponse && error.cause.status === 403) {
              this.loginError.set('Your account is not authorized to access this workspace.');
            } else {
              this.loginError.set('Could not load your account. Please try again.');
            }
          } else if (error.status === 401) {
            this.loginError.set('Invalid email or password.');
          } else if (error instanceof HttpErrorResponse && error.status === 429) {
            this.loginError.set('Too many sign-in attempts. Please wait before trying again.');
          } else {
            this.loginError.set('Something went wrong. Please try again.');
          }
        },
      });
  }
}
