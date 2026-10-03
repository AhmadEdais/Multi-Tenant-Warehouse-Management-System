export interface ProductListItem {
  id: number;
  sku: string;
  name: string;
  unitPrice: number;
  reorderPoint: number;
  isActive: boolean;
  categoryIds: number[];
}

export interface ProductCategory {
  categoryId: number;
  categoryName: string;
}

export interface ProductDetails {
  id: number;
  sku: string;
  name: string;
  description: string | null;
  unitOfMeasure: string;
  unitCost: number;
  unitPrice: number;
  reorderPoint: number;
  isActive: boolean;
  categories: ProductCategory[];
}

export interface ListProductsParams {
  searchTerm: string;
  categoryId: number | null;
  isActive: boolean | null;
  pageNumber: number;
  pageSize: number;
}

export interface ProductFields {
  name: string;
  description: string | null;
  unitOfMeasure: string;
  unitCost: number;
  unitPrice: number;
  reorderPoint: number;
}

export interface CreateProductRequest extends ProductFields {
  sku: string;
}

export type UpdateProductRequest = ProductFields;

export interface ProductFormValue {
  product: CreateProductRequest;
  categoryIds: number[];
}
