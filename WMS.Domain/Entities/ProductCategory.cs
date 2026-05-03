namespace WMS.Domain.Entities
{
    public class ProductCategory
    {
        public int ProductId { get; private set; }
        public int CategoryId { get; private set; }

        // Navigation properties
        public Product Product { get; private set; } = null!;
        public Category Category { get; private set; } = null!;

        // Required by EF Core
        private ProductCategory() { }

        public static ProductCategory Create(int productId, int categoryId)
        {
            return new ProductCategory
            {
                ProductId = productId,
                CategoryId = categoryId
            };
        }
    }
}