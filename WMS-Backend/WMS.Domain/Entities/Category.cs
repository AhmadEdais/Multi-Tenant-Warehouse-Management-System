using WMS.Domain.Interfaces; 

namespace WMS.Domain.Entities
{
    public class Category : IMustBelongToTenant
    {
        public int Id { get; private set; }
        public int TenantId { get; set; } 
        public int? ParentCategoryId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }

        public Category? ParentCategory { get; private set; }

        private readonly List<Category> _children = [];
        public IReadOnlyCollection<Category> Children => _children.AsReadOnly();

        private readonly List<ProductCategory> _productCategories = [];
        public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

        private Category() { }

        public static Category Create(string name, int? parentCategoryId = null)
        {
            return new Category
            {
                Name = name,
                ParentCategoryId = parentCategoryId,
                IsActive = true
            };
        }

        public void Update(string name, int? parentCategoryId)
        {
            Name = name;
            ParentCategoryId = parentCategoryId;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}