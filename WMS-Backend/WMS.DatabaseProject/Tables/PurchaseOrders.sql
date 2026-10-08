CREATE TABLE [dbo].[PurchaseOrders]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SupplierId] INT NOT NULL,
    [WarehouseId] INT NOT NULL,
    [OrderNumber] NVARCHAR(50) NOT NULL, -- e.g., 'PO-000124'
    [Status] TINYINT NOT NULL DEFAULT 1, -- 1=Draft, 2=Pending, 3=Receiving, 4=Received, 5=Canceled
    [ExpectedDeliveryDate] DATE NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- Standard Audit Columns
    [CreatedOnUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(128) NOT NULL,
    [LastModifiedOnUtc] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(128) NULL,

    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PurchaseOrders_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_PurchaseOrders_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers]([Id]),
    CONSTRAINT [FK_PurchaseOrders_Warehouses] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
    
    -- A PO Number must be unique within a single business (Tenant)
    CONSTRAINT [UQ_PurchaseOrders_Tenant_OrderNumber] UNIQUE ([TenantId], [OrderNumber])
)
GO

-- Index to quickly pull open POs for a specific supplier
CREATE NONCLUSTERED INDEX [IX_PurchaseOrders_Supplier_Status] ON [dbo].[PurchaseOrders] ([SupplierId], [Status])
GO
