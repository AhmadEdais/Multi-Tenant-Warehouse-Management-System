import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, SessionInitializationError } from '../auth.services';

@Component({
  selector: 'app-demo-launch',
  templateUrl: './demo-launch.html',
  styleUrl: './demo-launch.css',
})
export class DemoLaunch {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly variant = input<'primary' | 'secondary'>('primary');
  readonly isPreparing = signal(false);
  readonly error = signal<string | null>(null);
  readonly demoStarting = this.auth.isStartingDemo;

  start(): void {
    if (this.isPreparing() || this.demoStarting()) return;
    this.error.set(null);
    this.isPreparing.set(true);

    this.auth
      .startDemo()
      .pipe(
        finalize(() => this.isPreparing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/dashboard').then((navigated) => {
            if (!navigated)
              this.error.set(
                'Your workspace is ready, but navigation failed. Open the dashboard to continue.',
              );
          });
        },
        error: (error: unknown) => {
          if (error instanceof SessionInitializationError) {
            this.error.set(
              'Your demo was created, but your account could not be loaded. Reload the page to restore the session.',
            );
            return;
          }
          if (error instanceof HttpErrorResponse) {
            switch (error.status) {
              case 403:
                this.error.set(
                  'Demo creation was blocked. Sign out of any existing session and use the trusted site.',
                );
                return;
              case 429:
                this.error.set(
                  'Too many demo requests from this connection. Please wait before trying again.',
                );
                return;
              case 503:
                this.error.set(
                  'Demo creation is temporarily unavailable or busy. Please try again shortly.',
                );
                return;
            }
          }
          this.error.set('Could not prepare your demo workspace. Please try again.');
        },
      });
  }
}
