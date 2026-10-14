import { DecimalPipe } from '@angular/common';
import { Component, ElementRef, HostListener, input, output, ViewChild } from '@angular/core';
import { SalesOrderAvailabilityResult, SalesOrderDetails } from '../models/sales-order';

@Component({
  selector: 'app-sales-order-approval-dialog',
  imports: [DecimalPipe],
  templateUrl: './sales-order-approval-dialog.html',
})
export class SalesOrderApprovalDialogComponent {
  private dialogElement?: HTMLElement;
  readonly order = input.required<SalesOrderDetails>();
  readonly availability = input<SalesOrderAvailabilityResult | null>(null);
  readonly checking = input(false);
  readonly approving = input(false);
  readonly canApprove = input(false);
  readonly error = input<string | null>(null);
  readonly notice = input<string | null>(null);

  readonly close = output<void>();
  readonly retry = output<void>();
  readonly confirm = output<void>();
  readonly edit = output<void>();

  @ViewChild('dialog')
  set dialog(element: ElementRef<HTMLElement> | undefined) {
    this.dialogElement = element?.nativeElement;
    element?.nativeElement.focus();
  }

  availabilityFor(productId: number) {
    return this.availability()?.lines.find((line) => line.productId === productId);
  }

  requestClose(): void {
    if (!this.approving()) this.close.emit();
  }

  trapTab(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    const buttons = Array.from(
      this.dialogElement?.querySelectorAll<HTMLButtonElement>('button:not([disabled])') ?? [],
    );
    if (!buttons.length) {
      keyboardEvent.preventDefault();
      this.dialogElement?.focus();
      return;
    }
    const first = buttons[0];
    const last = buttons[buttons.length - 1];
    if (
      keyboardEvent.shiftKey &&
      (document.activeElement === first || document.activeElement === this.dialogElement)
    ) {
      keyboardEvent.preventDefault();
      last.focus();
    } else if (!keyboardEvent.shiftKey && document.activeElement === last) {
      keyboardEvent.preventDefault();
      first.focus();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.requestClose();
  }
}
