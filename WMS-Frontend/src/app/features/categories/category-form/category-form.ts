import { Component, computed, HostListener, input, OnInit, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryParentMenuItem } from '../category-parent-menu-item/category-parent-menu-item';
import { CategoryNode, CreateCategoryRequest } from '../models/category';

@Component({
  selector: 'app-category-form',
  imports: [ReactiveFormsModule, CategoryParentMenuItem],
  templateUrl: './category-form.html',
})
export class CategoryFormComponent implements OnInit {
  readonly mode = input.required<'add' | 'edit'>();
  readonly categoryTree = input.required<CategoryNode[]>();
  readonly category = input<CategoryNode | null>(null);
  readonly lockedParent = input<CategoryNode | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly close = output<void>();
  readonly submitCategory = output<CreateCategoryRequest>();

  readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    parentCategoryId: new FormControl<number | null>(null),
  });
  readonly pickerOpen = signal(false);
  readonly pickerOpenPath = signal<number[]>([]);
  readonly pickerPosition = signal({ top: 0, left: 0 });
  readonly openLeft = signal(false);

  ngOnInit(): void {
    this.form.reset({
      name: this.category()?.name ?? '',
      parentCategoryId: this.lockedParent()?.id ?? this.category()?.parentCategoryId ?? null,
    });
  }

  get parentName(): string {
    const id = this.form.controls.parentCategoryId.value;
    if (id === null) return 'None (Root)';
    return (
      this.findCategory(this.categoryTree(), id)?.name ??
      this.lockedParent()?.name ??
      'Select parent'
    );
  }

  readonly parentOptions = computed(() => {
    const excluded = new Set<number>();
    const edited = this.category();
    if (edited) {
      const source = this.findCategory(this.categoryTree(), edited.id);
      if (source) this.collectSubtreeIds(source, excluded);
      else excluded.add(edited.id);
    }
    const keepActive = (nodes: CategoryNode[]): CategoryNode[] =>
      nodes
        .filter((node) => node.isActive && !excluded.has(node.id))
        .map((node) => ({ ...node, children: keepActive(node.children) }));
    return keepActive(this.categoryTree());
  });

  togglePicker(event: MouseEvent): void {
    event.stopPropagation();
    if (this.lockedParent()) return;
    if (this.pickerOpen()) {
      this.pickerOpen.set(false);
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const left = Math.max(8, Math.min(rect.left, window.innerWidth - 232));
    this.pickerPosition.set({
      top: Math.max(8, Math.min(rect.bottom + 4, window.innerHeight - 270)),
      left,
    });
    this.openLeft.set(left + 456 > window.innerWidth);
    this.pickerOpenPath.set([]);
    this.pickerOpen.set(true);
  }

  chooseParent(parent: CategoryNode | null): void {
    this.form.controls.parentCategoryId.setValue(parent?.id ?? null);
    this.pickerOpen.set(false);
  }

  submit(): void {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    if (!value.name.trim()) {
      this.form.controls.name.setErrors({ required: true });
      this.form.controls.name.markAsTouched();
      return;
    }
    this.submitCategory.emit({ name: value.name.trim(), parentCategoryId: value.parentCategoryId });
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    this.pickerOpen.set(false);
  }

  private findCategory(nodes: CategoryNode[], id: number): CategoryNode | null {
    for (const node of nodes) {
      if (node.id === id) return node;
      const child = this.findCategory(node.children, id);
      if (child) return child;
    }
    return null;
  }

  private collectSubtreeIds(node: CategoryNode, ids: Set<number>): void {
    ids.add(node.id);
    for (const child of node.children) this.collectSubtreeIds(child, ids);
  }
}
