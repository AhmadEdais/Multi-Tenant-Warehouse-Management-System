import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../auth.services';
import { LoginRequest } from '../models/login-request';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login {
  private authService = inject(AuthService);
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
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    const { email, password } = this.loginForm.getRawValue();
    const request: LoginRequest = {
      email,
      password,
    };
    this.authService.login(request).subscribe({
      next: (response) => {
        console.log('Login successful:', response);
        // Handle successful login, e.g., store token, navigate to dashboard, etc.
      },
      error: (error) => {
        console.error('Login failed:', error);
        // Handle login error, e.g., show error message to user
      },
    });
  }
  passwordVisible = false;

  togglePasswordVisibility() {
    this.passwordVisible = !this.passwordVisible;
    console.log('Password visibility toggled:', this.passwordVisible);
  }
}
