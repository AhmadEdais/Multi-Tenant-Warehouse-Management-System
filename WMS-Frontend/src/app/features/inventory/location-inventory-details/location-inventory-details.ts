import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AfterViewInit,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  input,
  OnInit,
  output,
  signal,
  ViewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of, Subject, switchMap } from 'rxjs';
import { InventoryService } from '../inventory.service';
import { StockByLocation } from '../models/inventory';

@Component({
  selector: 'app-location-inventory-details',
  imports: [DecimalPipe],
  templateUrl: './location-inventory-details.html',
})
export class LocationInventoryDetailsComponent implements OnInit, AfterViewInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  @ViewChild('closeButton') private closeButton?: ElementRef<HTMLButtonElement>;

  readonly locationId = input.required<number>();
  readonly locationName = input('Location');
  readonly close = output<void>();

  readonly pageSize = 10;
  readonly page = signal(1);
  readonly stock = signal<StockByLocation[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly firstItem = computed(() =>
    this.totalCount() ? (this.page() - 1) * this.pageSize + 1 : 0,
  );
  readonly lastItem = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));

  ngOnInit(): void {
    this.reload$
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.inventoryService
            .getByLocation(this.locationId(), this.page(), this.pageSize)
            .pipe(
              catchError((error: HttpErrorResponse) => {
                this.error.set(
                  error.status === 404
                    ? 'This location is no longer available.'
                    : 'Could not load location inventory. Please try again.',
                );
                return of(null);
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        this.stock.set(result?.data ?? []);
        this.totalCount.set(result?.totalCount ?? 0);
        if (result && this.page() > this.pageCount()) {
          this.page.set(this.pageCount());
          this.reload$.next();
        }
      });
    this.reload$.next();
  }

  ngAfterViewInit(): void {
    this.closeButton?.nativeElement.focus();
  }

  goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.pageCount() || page === this.page()) return;
    this.page.set(page);
    this.reload$.next();
  }

  retry(): void {
    this.reload$.next();
  }

  dismiss(): void {
    this.close.emit();
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event): void {
    event.stopImmediatePropagation();
    this.dismiss();
  }
}
