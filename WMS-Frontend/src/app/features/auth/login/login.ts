import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../auth.services';
import { LoginRequest } from '../models/login-request';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { TextField } from '../../../shared/components/text-field/text-field';
import { PasswordField } from '../../../shared/components/password-field/password-field';

@Component({
  imports: [ReactiveFormsModule, RouterLink, TextField, PasswordField],
  selector: 'app-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);
  loginError = signal<string | null>(null);
  isSubmitting = signal(false);

  private handleSuccessfulLogin(token: string) {
    const rememberMe = this.loginForm.controls.rememberMe.value;
    localStorage.removeItem('authToken');
    this.authService.saveToken(token, rememberMe);
    this.router.navigate(['/dashboard']);
  }
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
    this.isSubmitting.set(true);

    this.authService
      .login(request)
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: (response) => {
          this.handleSuccessfulLogin(response.token);
        },

        error: (error) => {
          if (error.status === 401) {
            this.loginError.set('Invalid email or password.');
          } else {
            this.loginError.set('Something went wrong. Please try again.');
          }
        },
      });
  }
}
