import { Component, computed, input, OnInit, output, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CategoryNode } from '../../categories/models/category';
import { ProductDetails, ProductFormValue } from '../models/product';

interface CategoryOption {
  id: number;
  name: string;
  depth: number;
  isActive: boolean;
}

const requiredText = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && !control.value.trim() ? { required: true } : null;

const wholeNumber = (control: AbstractControl): ValidationErrors | null =>
  Number.isInteger(control.value) ? null : { integer: true };

const decimalAmount = Validators.pattern(/^\d{1,14}(\.\d{1,4})?$/);

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule],
  templateUrl: './product-form.html',
})
export class ProductFormComponent implements OnInit {
  readonly mode = input.required<'create' | 'edit'>();
  readonly product = input<ProductDetails | null>(null);
  readonly categoryTree = input<CategoryNode[]>([]);
  readonly categoriesLoading = input(false);
  readonly categoriesError = input<string | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly pendingCreatedId = input<number | null>(null);
  readonly close = output<void>();
  readonly save = output<ProductFormValue>();
  readonly retryCategories = output<void>();

  readonly form = new FormGroup({
    sku: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(100)],
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(200)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)],
    }),
    unitOfMeasure: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(50)],
    }),
    unitCost: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0), decimalAmount],
    }),
    unitPrice: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0), decimalAmount],
    }),
    reorderPoint: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0), wholeNumber],
    }),
  });
  readonly selectedCategoryIds = signal<Set<number>>(new Set());
  readonly categoryOptions = computed(() => {
    const options: CategoryOption[] = [];
    const visit = (nodes: CategoryNode[], depth: number): void => {
      for (const node of nodes) {
        if (node.isActive || this.selectedCategoryIds().has(node.id)) {
          options.push({ id: node.id, name: node.name, depth, isActive: node.isActive });
        }
        visit(node.children, depth + 1);
      }
    };
    visit(this.categoryTree(), 0);
    return options;
  });

  ngOnInit(): void {
    const product = this.product();
    if (product) {
      this.form.reset({
        sku: product.sku,
        name: product.name,
        description: product.description ?? '',
        unitOfMeasure: product.unitOfMeasure,
        unitCost: product.unitCost,
        unitPrice: product.unitPrice,
        reorderPoint: product.reorderPoint,
      });
      this.selectedCategoryIds.set(
        new Set(product.categories.map((category) => category.categoryId)),
      );
    }
  }

  toggleCategory(id: number): void {
    if (this.saving()) return;
    this.selectedCategoryIds.update((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  submit(): void {
    if (this.saving()) return;
    if (this.form.invalid && this.pendingCreatedId() === null) {
      this.form.markAllAsTouched();
      return;
    }
    const values = this.form.getRawValue();
    this.save.emit({
      product: {
        sku: values.sku.trim(),
        name: values.name.trim(),
        description: values.description.trim() || null,
        unitOfMeasure: values.unitOfMeasure.trim(),
        unitCost: values.unitCost,
        unitPrice: values.unitPrice,
        reorderPoint: values.reorderPoint,
      },
      categoryIds: [...this.selectedCategoryIds()],
    });
  }
}
