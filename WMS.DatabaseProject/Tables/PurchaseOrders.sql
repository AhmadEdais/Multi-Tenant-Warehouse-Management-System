CREATE TABLE [dbo].[PurchaseOrders]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SupplierId] INT NOT NULL,
    
    [OrderNumber] NVARCHAR(50) NOT NULL, -- e.g., 'PO-2026-001'
    [Status] TINYINT NOT NULL DEFAULT 1, -- 1=Pending, 2=Receiving, 3=Received, 4=Canceled
    [ExpectedDeliveryDate] DATE NULL,

    -- Standard Audit Columns
    [CreatedOnUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(128) NOT NULL,
    [LastModifiedOnUtc] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(128) NULL,

    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PurchaseOrders_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_PurchaseOrders_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers]([Id]),
    
    -- A PO Number must be unique within a single business (Tenant)
    CONSTRAINT [UQ_PurchaseOrders_Tenant_OrderNumber] UNIQUE ([TenantId], [OrderNumber])
)
GO

-- Index to quickly pull open POs for a specific supplier
CREATE NONCLUSTERED INDEX [IX_PurchaseOrders_Supplier_Status] ON [dbo].[PurchaseOrders] ([SupplierId], [Status])
GO