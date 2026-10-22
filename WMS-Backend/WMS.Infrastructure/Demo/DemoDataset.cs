using WMS.Domain.Enums;

namespace WMS.Infrastructure.Demo;

internal static class DemoDataset
{
    internal sealed record WarehouseData(string Code, string Name);
    internal sealed record CategoryData(string Root, IReadOnlyList<string> Children);
    internal sealed record ProductData(string Sku, string Name, string Category, string Unit,
        decimal Cost, decimal Price, int Reorder, string Bin, decimal DockRemainder = 0);
    internal sealed record PartnerData(string Code, string Name);
    internal sealed record PurchaseLine(string Sku, decimal Expected, decimal Received = 0);
    internal sealed record PurchaseData(string Supplier, string Warehouse, PurchaseOrderStatus Status,
        IReadOnlyList<PurchaseLine> Lines);
    internal sealed record SalesLine(string Sku, decimal Quantity, decimal Pickup = 0, string? AllocationBin = null);
    internal sealed record SalesData(string Customer, string Warehouse, SalesOrderStatus Status, IReadOnlyList<SalesLine> Lines);

    internal static IReadOnlyList<WarehouseData> Warehouses { get; } = Array.AsReadOnly<WarehouseData>([
        new("MAIN", "Main Distribution Center"), new("OVERFLOW", "Bulk Storage Warehouse")]);
    internal static IReadOnlyList<CategoryData> Categories { get; } = Array.AsReadOnly<CategoryData>([
        new("Warehouse Technology", ["Barcode Scanners", "Label Printers", "Scanner Accessories"]),
        new("Packaging Materials", ["Cardboard Boxes", "Shipping Labels", "Tape & Stretch Wrap"]),
        new("Storage & Safety", ["Storage Containers", "Warehouse Safety"]),
        new("Office Supplies", ["Stationery", "Desk Equipment"])]);
    internal static IReadOnlyList<ProductData> Products { get; } = Array.AsReadOnly<ProductData>([
        new("SCN-001", "Wireless Barcode Scanner", "Barcode Scanners", "Each", 45, 75, 10, "A1-R1-B1", 10),
        new("SCN-002", "Industrial Barcode Scanner", "Barcode Scanners", "Each", 95, 145, 6, "A1-R2-B1"),
        new("SCN-003", "Handheld 2D Scanner", "Barcode Scanners", "Each", 65, 99, 8, "A1-R2-B2"),
        new("PRN-001", "Thermal Label Printer", "Label Printers", "Each", 110, 165, 5, "A2-R1-B1"),
        new("PRN-002", "Industrial Label Printer", "Label Printers", "Each", 225, 340, 3, "A2-R1-B2"),
        new("ACC-001", "Scanner Charging Dock", "Scanner Accessories", "Each", 22, 38, 10, "A2-R2-B1"),
        new("ACC-002", "Scanner Holster", "Scanner Accessories", "Each", 12, 20, 12, "A2-R2-B2"),
        new("ACC-003", "USB Scanner Cable", "Scanner Accessories", "Each", 5, 10, 20, "A2-R2-B2"),
        new("BOX-001", "Small Shipping Box", "Cardboard Boxes", "Each", .25m, .45m, 150, "B1-R1-B1"),
        new("BOX-002", "Medium Shipping Box", "Cardboard Boxes", "Each", .45m, .85m, 120, "B1-R1-B2"),
        new("BOX-003", "Large Shipping Box", "Cardboard Boxes", "Each", .85m, 1.50m, 80, "B1-R2-B1"),
        new("BOX-004", "Heavy Duty Carton", "Cardboard Boxes", "Each", 1.20m, 2.10m, 60, "B1-R2-B2"),
        new("LBL-001", "Thermal Shipping Labels", "Shipping Labels", "Roll", 2.10m, 4, 40, "B2-R1-B1", 40),
        new("LBL-002", "Fragile Stickers", "Shipping Labels", "Roll", .70m, 1.50m, 35, "B2-R1-B1"),
        new("LBL-003", "Warehouse Shelf Labels", "Shipping Labels", "Pack", 3, 5, 25, "B2-R1-B2"),
        new("TAP-001", "Packing Tape", "Tape & Stretch Wrap", "Roll", .90m, 1.75m, 60, "B2-R2-B1", 30),
        new("WRAP-001", "Stretch Wrap", "Tape & Stretch Wrap", "Roll", 4.20m, 7.50m, 25, "B2-R2-B1"),
        new("WRAP-002", "Bubble Wrap Roll", "Tape & Stretch Wrap", "Roll", 3.80m, 6.80m, 25, "B2-R2-B2"),
        new("BIN-001", "Plastic Storage Tote", "Storage Containers", "Each", 6, 10, 20, "B1-R2-B1"),
        new("BIN-002", "Stackable Bin Box", "Storage Containers", "Each", 3.50m, 6.50m, 25, "B1-R2-B2"),
        new("BIN-003", "Heavy Duty Crate", "Storage Containers", "Each", 14, 25, 10, "B2-R2-B2"),
        new("SAF-001", "Safety Gloves", "Warehouse Safety", "Pair", 2.80m, 5, 20, "C1-R1-B1", 20),
        new("SAF-002", "High Visibility Vest", "Warehouse Safety", "Each", 5.50m, 10, 15, "C1-R1-B1"),
        new("SAF-003", "Safety Goggles", "Warehouse Safety", "Each", 3.20m, 6, 12, "C1-R1-B2"),
        new("OFF-001", "A4 Printer Paper", "Stationery", "Pack", 3.80m, 6.50m, 20, "A2-R1-B1", 30),
        new("OFF-002", "Permanent Marker Pack", "Stationery", "Pack", 2.20m, 4.50m, 20, "A2-R1-B2"),
        new("OFF-003", "Shipping Clipboard", "Stationery", "Each", 2, 4, 10, "A2-R2-B1"),
        new("OFF-004", "Desktop Label Dispenser", "Desk Equipment", "Each", 13, 22, 6, "A2-R2-B2")]);
    internal static IReadOnlyList<PartnerData> Suppliers { get; } = Array.AsReadOnly<PartnerData>([
        new("TECH", "TechSupply Co."), new("PACK", "PackRight Ltd."), new("OFFICE", "Office Essentials"),
        new("SAFETY", "SafetyFirst Supplies"), new("STORAGE", "Warehouse Components")]);
    internal static IReadOnlyList<PartnerData> Customers { get; } = Array.AsReadOnly<PartnerData>([
        new("CUST-001", "Amman Retail Group"), new("CUST-002", "Jordan Online Stores"),
        new("CUST-003", "Capital Logistics"), new("CUST-004", "City Office Supplies"),
        new("CUST-005", "Levant E-Commerce"), new("CUST-006", "Regional Fulfillment Services")]);

    internal static IReadOnlyList<PurchaseData> Purchases { get; } = Array.AsReadOnly<PurchaseData>([
        new("TECH", "MAIN", PurchaseOrderStatus.Received, [
            new("SCN-001",100,100), new("SCN-002",4,4), new("SCN-003",40,40), new("PRN-001",25,25),
            new("PRN-002",2,2), new("ACC-001",50,50), new("ACC-002",60,60), new("ACC-003",100,100)]),
        new("PACK", "MAIN", PurchaseOrderStatus.Received, [
            new("BOX-001",800,800), new("BOX-002",500,500), new("BOX-003",240,240), new("BOX-004",180,180),
            new("LBL-001",240,240), new("LBL-002",160,160), new("LBL-003",100,100), new("TAP-001",180,180),
            new("WRAP-001",90,90), new("WRAP-002",80,80)]),
        new("STORAGE", "MAIN", PurchaseOrderStatus.Received, [new("BIN-001",80,80),new("BIN-002",100,100),
            new("BIN-003",40,40),new("OFF-002",80,80),new("OFF-003",40,40),new("OFF-004",6,6)]),
        new("OFFICE", "MAIN", PurchaseOrderStatus.Receiving, [new("OFF-001",200,150)]),
        new("SAFETY", "OVERFLOW", PurchaseOrderStatus.Receiving, [new("SAF-001",150,100),new("SAF-002",100,60),
            new("SAF-003",20,6),new("BOX-001",200,200),new("BOX-002",100,100),new("BIN-001",30,30)]),
        new("TECH", "MAIN", PurchaseOrderStatus.Pending, [new("PRN-002",10),new("SCN-002",20)]),
        new("PACK", "OVERFLOW", PurchaseOrderStatus.Pending, [new("BOX-003",200),new("WRAP-001",80)]),
        new("STORAGE", "MAIN", PurchaseOrderStatus.Draft, [new("BIN-001",60),new("BIN-002",80)]),
        new("OFFICE", "MAIN", PurchaseOrderStatus.Draft, [new("OFF-004",20),new("OFF-003",30)]),
        new("PACK", "MAIN", PurchaseOrderStatus.Canceled, [new("BOX-004",50),new("TAP-001",100)])]);

    internal static IReadOnlyList<SalesData> Sales { get; } = Array.AsReadOnly<SalesData>([
        new("CUST-001","MAIN",SalesOrderStatus.Draft,[new("PRN-001",3),new("ACC-001",6)]),
        new("CUST-002","MAIN",SalesOrderStatus.Draft,[new("BOX-001",100),new("LBL-001",20)]),
        new("CUST-003","MAIN",SalesOrderStatus.Picked,[new("SCN-001",8,5,"A1-R1-B1")]),
        new("CUST-005","MAIN",SalesOrderStatus.Picked,[new("BOX-001",100,40),new("TAP-001",20,10)]),
        new("CUST-004","MAIN",SalesOrderStatus.Allocated,[new("OFF-001",20),new("OFF-002",10)]),
        new("CUST-006","OVERFLOW",SalesOrderStatus.Allocated,[new("SAF-001",10),new("BIN-001",10)]),
        new("CUST-001","MAIN",SalesOrderStatus.Allocated,[new("SCN-003",10),new("PRN-001",5)]),
        new("CUST-004","MAIN",SalesOrderStatus.Shipped,[new("OFF-004",6,6),new("ACC-003",20,20)]),
        new("CUST-002","OVERFLOW",SalesOrderStatus.Shipped,[new("BOX-002",60,60)]),
        new("CUST-005","MAIN",SalesOrderStatus.Canceled,[new("WRAP-001",10)])]);

    internal static IReadOnlyList<(string Bin, decimal Quantity)> Putaway(string warehouse, ProductData product,
        decimal received)
    {
        if (product.Sku == "SCN-001") return [("A1-R1-B1",60),("A1-R1-B2",30)];
        if (warehouse == "OVERFLOW" && product.Sku.StartsWith("BOX-")) return [("C2-R1-B1", received)];
        if (warehouse == "OVERFLOW" && product.Sku == "BIN-001") return [("C2-R1-B2", received)];
        if (warehouse == "MAIN" && product.Sku == "BOX-001") return [(product.Bin,600),("B2-R2-B2",200)];
        return [(product.Bin, received - product.DockRemainder)];
    }
}
