using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using WMS.Application.Features.Demo.Interfaces;
using WMS.Domain.Constants;

namespace WMS.Infrastructure.Demo;

internal sealed partial class DemoDataSeeder(WmsDbContext context, ILogger<DemoDataSeeder> logger)
    : IDemoDataSeeder
{
    private readonly Dictionary<string, Warehouse> warehouses = [];
    private readonly Dictionary<string, Location> locations = [];
    private readonly Dictionary<string, Category> categories = [];
    private readonly Dictionary<string, Product> products = [];
    private readonly Dictionary<string, Supplier> suppliers = [];
    private readonly Dictionary<string, Customer> customers = [];
    private readonly Dictionary<(int Product, int Location), StockLevel> stock = [];
    private readonly List<PurchaseOrder> purchases = [];
    private readonly List<SalesOrder> sales = [];
    private PurchaseOrderSequence poSequence = null!;
    private SalesOrderSequence soSequence = null!;
    private int tenantId;
    private string actor = "";
    private int checkpoints;
    private double saveMilliseconds;

    public async Task SeedAsync(int tenantId, int adminUserId, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null || this.tenantId != 0)
            throw new InvalidOperationException("Demo seeding requires a fresh scoped seeder and an outer transaction.");
        this.tenantId = tenantId;
        actor = adminUserId.ToString(CultureInfo.InvariantCulture);
        try
        {
            await StageAsync("Warehouses and docks", SeedWarehousesAsync, cancellationToken);
            await StageAsync("Locations", SeedLocationsAsync, cancellationToken);
            await StageAsync("Categories", SeedCategoriesAsync, cancellationToken);
            await StageAsync("Products and mappings", SeedProductsAsync, cancellationToken);
            await StageAsync("Suppliers and customers", SeedPartnersAsync, cancellationToken);
            await StageAsync("Purchase orders", SeedPurchasesAsync, cancellationToken);
            await StageAsync("Receiving", ReceiveAsync, cancellationToken);
            await StageAsync("Putaway", PutawayAsync, cancellationToken);
            await StageAsync("Sales orders", SeedSalesAsync, cancellationToken);
            await StageAsync("Allocations", AllocateAsync, cancellationToken);
            await StageAsync("Pickups", PickupAsync, cancellationToken);
            await StageAsync("Consistency verification", VerifyConsistencyAsync, cancellationToken);
        }
        finally
        {
            logger.LogInformation("[Demo Performance] Seeder SaveChanges: {ElapsedMilliseconds:F2} ms; checkpoints: {Checkpoints}",
                saveMilliseconds, checkpoints);
        }
    }

    // TODO(DEMO-PERF): Remove Stopwatch instrumentation after measuring and optimizing demo provisioning.
    private async Task StageAsync(string stage, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();
        var savesBefore = saveMilliseconds;
        try { await action(ct); }
        catch
        {
            logger.LogWarning("[Demo Performance] Stage failed: {Stage}", stage);
            throw;
        }
        finally
        {
            logger.LogInformation("[Demo Performance] {Stage}: {ElapsedMilliseconds:F2} ms; SaveChanges: {SaveMilliseconds:F2} ms; other work: {OtherMilliseconds:F2} ms",
                stage, timer.Elapsed.TotalMilliseconds, saveMilliseconds - savesBefore,
                Math.Max(0, timer.Elapsed.TotalMilliseconds - (saveMilliseconds - savesBefore)));
        }
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();
        checkpoints++;
        try { await context.SaveChangesAsync(ct); }
        finally { saveMilliseconds += timer.Elapsed.TotalMilliseconds; }
    }

    private async Task SeedWarehousesAsync(CancellationToken ct)
    {
        foreach (var data in DemoDataset.Warehouses)
        {
            var warehouse = Warehouse.Create(tenantId, data.Code, data.Name, "Amman, Jordan");
            warehouses.Add(data.Code, warehouse);
            context.Warehouses.Add(warehouse);
        }
        await SaveAsync(ct);
        foreach (var warehouse in warehouses.Values)
            AddLocation(warehouse.Code, "DOCK", LocationTypes.Dock, "Receiving Dock", null);
        await SaveAsync(ct);
    }

    private async Task SeedLocationsAsync(CancellationToken ct)
    {
        foreach (var zone in new[] { ("MAIN", "A", "Technology and Office"),
                     ("MAIN", "B", "Packaging and Safety"), ("OVERFLOW", "C", "Bulk Storage") })
            AddLocation(zone.Item1, zone.Item2, LocationTypes.Zone, $"Zone {zone.Item2} - {zone.Item3}", null);
        await SaveAsync(ct);
        foreach (var warehouse in warehouses.Keys)
        foreach (var zone in warehouse == "MAIN" ? new[] { "A", "B" } : new[] { "C" })
        for (var aisle = 1; aisle <= 2; aisle++)
            AddLocation(warehouse, $"{zone}{aisle}", LocationTypes.Aisle, $"Aisle {zone}{aisle}", $"{warehouse}-{zone}");
        await SaveAsync(ct);
        foreach (var aisle in locations.Values.Where(l => l.LocationType == LocationTypes.Aisle).ToList())
        {
            var warehouse = warehouses.Values.Single(w => w.Id == aisle.WarehouseId);
            var code = aisle.Barcode![(warehouse.Code.Length + 1)..];
            for (var rack = 1; rack <= (warehouse.Code == "MAIN" ? 2 : 1); rack++)
                AddLocation(warehouse.Code, $"{code}-R{rack}", LocationTypes.Rack, $"Rack {code}-R{rack}", aisle.Barcode);
        }
        await SaveAsync(ct);
        foreach (var rack in locations.Values.Where(l => l.LocationType == LocationTypes.Rack).ToList())
        {
            var warehouse = warehouses.Values.Single(w => w.Id == rack.WarehouseId);
            var code = rack.Barcode![(warehouse.Code.Length + 1)..];
            for (var bin = 1; bin <= 2; bin++)
                AddLocation(warehouse.Code, $"{code}-B{bin}", LocationTypes.Bin, $"Bin {code}-B{bin}", rack.Barcode);
        }
        await SaveAsync(ct);
    }

    private void AddLocation(string warehouseCode, string code, string type, string name, string? parentKey)
    {
        var warehouse = warehouses[warehouseCode];
        var parent = parentKey is null ? null : locations[parentKey];
        if (type != LocationTypes.Dock && LocationTypes.RequiredParentType(type) != parent?.LocationType
            || parent is not null && (parent.TenantId != tenantId || parent.WarehouseId != warehouse.Id))
            throw new InvalidOperationException("Invalid demo location hierarchy.");
        var key = $"{warehouseCode}-{code}";
        var location = Location.Create(warehouse.Id, parent?.Id, type, name, key, null);
        location.TenantId = tenantId;
        locations.Add(key, location);
        context.Locations.Add(location);
    }

    private async Task SeedCategoriesAsync(CancellationToken ct)
    {
        foreach (var data in DemoDataset.Categories) AddCategory(data.Root, null);
        await SaveAsync(ct);
        foreach (var data in DemoDataset.Categories)
        foreach (var child in data.Children) AddCategory(child, categories[data.Root].Id);
        await SaveAsync(ct);
    }

    private void AddCategory(string name, int? parentId)
    {
        var category = Category.Create(name, parentId);
        category.TenantId = tenantId;
        categories.Add(name, category);
        context.Categories.Add(category);
    }

    private async Task SeedProductsAsync(CancellationToken ct)
    {
        foreach (var data in DemoDataset.Products)
        {
            var product = Product.Create(data.Sku, data.Name,
                $"{data.Name} for distribution, warehouse operations and commercial customers.",
                data.Unit, data.Cost, data.Price, data.Reorder);
            product.TenantId = tenantId;
            products.Add(data.Sku, product);
            context.Products.Add(product);
        }
        await SaveAsync(ct);
        context.ProductCategories.AddRange(DemoDataset.Products.Select(data =>
            ProductCategory.Create(products[data.Sku].Id, categories[data.Category].Id)));
        await SaveAsync(ct);
    }

    private async Task SeedPartnersAsync(CancellationToken ct)
    {
        foreach (var data in DemoDataset.Suppliers)
        {
            var supplier = Supplier.Create(data.Code, data.Name, $"orders@{data.Code.ToLowerInvariant()}.example",
                "+96265550100", "Amman, Jordan");
            supplier.TenantId = tenantId;
            suppliers.Add(data.Code, supplier);
            context.Suppliers.Add(supplier);
        }
        foreach (var data in DemoDataset.Customers)
        {
            var customer = Customer.Create(data.Code, data.Name, $"purchasing@{data.Code.ToLowerInvariant()}.example",
                "+96265550200", "Amman, Jordan", 25000m);
            customer.TenantId = tenantId;
            customers.Add(data.Code, customer);
            context.Customers.Add(customer);
        }
        await SaveAsync(ct);
    }
}
