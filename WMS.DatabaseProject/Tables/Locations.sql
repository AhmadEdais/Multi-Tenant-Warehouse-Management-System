CREATE TABLE [dbo].[Locations]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [WarehouseId] INT NOT NULL,
    [ParentLocationId] INT NULL, -- The Self-Referencing Tree Pointer
    [LocationType] NVARCHAR(50) NOT NULL, -- e.g., 'Zone', 'Aisle', 'Rack', 'Shelf', 'Bin'
    [LocationFunction] TINYINT NOT NULL DEFAULT 1, -- 1 = Storage, 2 = Dock, 3 = Dispatch
    [Name] NVARCHAR(100) NOT NULL, -- e.g., 'Aisle 5'
    [Barcode] NVARCHAR(100) NULL, -- Often only the lowest level (Bin) has a scannable barcode
    [MaxWeightCapacityKg] DECIMAL(18, 2) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Locations_IsActive] DEFAULT (1),

    CONSTRAINT [PK_Locations] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Locations_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_Locations_Warehouses] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
    CONSTRAINT [FK_Locations_ParentLocation] FOREIGN KEY ([ParentLocationId]) REFERENCES [dbo].[Locations]([Id]),
    
    -- A barcode must still be unique within a warehouse if it exists
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Locations_WarehouseId_Barcode] 
ON [dbo].[Locations] ([WarehouseId], [Barcode]) 
WHERE [Barcode] IS NOT NULL;