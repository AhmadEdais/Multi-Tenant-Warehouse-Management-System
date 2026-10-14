CREATE TABLE [dbo].[SalesOrders]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [CustomerId] INT NOT NULL,
    [WarehouseId] INT NOT NULL,
    [OrderNumber] NVARCHAR(50) NOT NULL,
    [Status] TINYINT NOT NULL CONSTRAINT [DF_SalesOrders_Status] DEFAULT (1), -- 1=Draft, 2=Allocated, 3=Shipped, 4=Canceled, 5=Picked
    [RequestedShipmentDate] DATE NULL,
    [RowVersion] ROWVERSION NOT NULL,
    [CreatedOnUtc] DATETIME2 NOT NULL CONSTRAINT [DF_SalesOrders_CreatedOnUtc] DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(128) NOT NULL,
    [LastModifiedOnUtc] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(128) NULL,

    CONSTRAINT [PK_SalesOrders] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SalesOrders_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_SalesOrders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]),
    CONSTRAINT [FK_SalesOrders_Warehouses] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
    CONSTRAINT [UQ_SalesOrders_Tenant_OrderNumber] UNIQUE ([TenantId], [OrderNumber]),
    CONSTRAINT [CK_SalesOrders_Status] CHECK ([Status] IN (1, 2, 3, 4, 5))
)
GO

CREATE NONCLUSTERED INDEX [IX_SalesOrders_Customer_Status]
ON [dbo].[SalesOrders] ([CustomerId], [Status])
GO
