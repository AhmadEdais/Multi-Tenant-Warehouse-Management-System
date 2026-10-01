export interface CategoryNode {
  id: number;
  parentCategoryId: number | null;
  name: string;
  isActive: boolean;
  level: number;
  children: CategoryNode[];
}

export interface CategoryDetails {
  id: number;
  name: string;
  parentCategoryId: number | null;
  parentCategoryName: string | null;
  isActive: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  parentCategoryId: number | null;
}

export type UpdateCategoryRequest = CreateCategoryRequest;
