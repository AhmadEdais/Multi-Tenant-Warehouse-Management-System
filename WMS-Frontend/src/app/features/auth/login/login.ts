import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../auth.services';
import { LoginRequest } from '../models/login-request';
import { Router } from '@angular/router';
@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);
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
        this.handleSuccessfulLogin(response.token);
      },
      error: (error) => {
        // Handle login error, e.g., show error message to user
        console.error('Login failed', error);
      },
    });
  }
  passwordVisible = false;

  togglePasswordVisibility() {
    this.passwordVisible = !this.passwordVisible;
  }
}
