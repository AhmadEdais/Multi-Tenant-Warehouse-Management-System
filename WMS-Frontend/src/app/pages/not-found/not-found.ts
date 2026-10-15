import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService, landingRouteFor } from '../../features/auth/auth.services';

@Component({
  imports: [RouterLink],
  selector: 'app-not-found',
  styleUrl: './not-found.css',
  templateUrl: './not-found.html',
})
export class NotFound {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly previousNavigation = this.router.getCurrentNavigation()?.previousNavigation;
  private readonly previousRoute = this.previousNavigation?.finalUrl
    ? this.router.serializeUrl(this.previousNavigation.finalUrl)
    : null;

  readonly sessionReady = signal(!this.authService.getToken());
  readonly isSignedIn = computed(() => this.authService.currentUser() !== null);
  readonly homeRoute = computed(() => {
    const user = this.authService.currentUser();
    return user ? landingRouteFor(user) : '/';
  });

  constructor() {
    if (this.authService.getToken()) {
      this.authService
        .ensureCurrentUser()
        .pipe(takeUntilDestroyed())
        .subscribe({
          next: () => this.sessionReady.set(true),
          error: () => this.sessionReady.set(true),
        });
    }
  }

  goBack(): void {
    void this.router.navigateByUrl(this.previousRoute ?? this.homeRoute());
  }
}
