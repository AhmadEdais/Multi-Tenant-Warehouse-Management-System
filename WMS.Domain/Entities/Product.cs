using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities
{
    public class Product : IMustBelongToTenant
    {
        public int Id { get; private set; }
        public int TenantId { get; set; } 
        public string SKU { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public string UnitOfMeasure { get; private set; } = string.Empty;
        public decimal UnitCost { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int ReorderPoint { get; private set; }
        public bool IsActive { get; private set; }
        public byte[] RowVersion { get; private set; } = null!;

        
        private readonly List<ProductCategory> _productCategories = [];
        public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

        private Product() { }

        public static Product Create(
            string sku,
            string name,
            string? description,
            string unitOfMeasure,
            decimal unitCost,
            decimal unitPrice,
            int reorderPoint)
        {
            return new Product
            {
                SKU = sku,
                Name = name,
                Description = description,
                UnitOfMeasure = unitOfMeasure,
                UnitCost = unitCost,
                UnitPrice = unitPrice,
                ReorderPoint = reorderPoint,
                IsActive = true
            };
        }

        public void Update(
            string name,
            string? description,
            string unitOfMeasure,
            decimal unitCost,
            decimal unitPrice,
            int reorderPoint)
        {
            Name = name;
            Description = description;
            UnitOfMeasure = unitOfMeasure;
            UnitCost = unitCost;
            UnitPrice = unitPrice;
            ReorderPoint = reorderPoint;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}