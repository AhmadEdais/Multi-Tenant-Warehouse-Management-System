using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities
{
    public class Location : IMustBelongToTenant
    {
        public int Id { get; private set; }
        public int TenantId { get; set; }
        public int WarehouseId { get; private set; }
        public int? ParentLocationId { get; private set; }

        public string LocationType { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string? Barcode { get; private set; }

        public decimal? MaxWeightCapacityKg { get; private set; }
        public bool IsActive { get; private set; }

        // Navigation properties
        public Warehouse? Warehouse { get; private set; }
        public Location? ParentLocation { get; private set; }
        public IReadOnlyCollection<Location> ChildLocations { get; private set; } = [];

        private Location() { }

        // Static Factory Method
        public static Location Create(
            int warehouseId,
            int? parentLocationId,
            string locationType,
            string name,
            string? barcode,
            decimal? maxWeightCapacityKg)
        {
            return new Location
            {
                WarehouseId = warehouseId,
                ParentLocationId = parentLocationId,
                LocationType = locationType,
                Name = name,
                Barcode = barcode,
                MaxWeightCapacityKg = maxWeightCapacityKg,
                IsActive = true
            };
        }

        public void Update(string name, string? barcode, decimal? maxWeightCapacityKg)
        {
            Name = name;
            Barcode = barcode;
            MaxWeightCapacityKg = maxWeightCapacityKg;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}