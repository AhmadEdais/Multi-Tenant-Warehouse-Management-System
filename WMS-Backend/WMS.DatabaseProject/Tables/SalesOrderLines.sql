CREATE TABLE [dbo].[SalesOrderLines]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SalesOrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] DECIMAL(18,2) NOT NULL,
    [QuantityAllocated] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SalesOrderLines_QuantityAllocated] DEFAULT (0),
    [UnitPrice] DECIMAL(18,4) NOT NULL,

    CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SalesOrderLines_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_SalesOrderLines_SalesOrders] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalesOrderLines_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
    CONSTRAINT [UQ_SalesOrderLines_SO_Product] UNIQUE ([SalesOrderId], [ProductId]),
    CONSTRAINT [CHK_SalesOrderLines_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CHK_SalesOrderLines_QuantityAllocated] CHECK ([QuantityAllocated] >= 0 AND [QuantityAllocated] <= [Quantity]),
    CONSTRAINT [CHK_SalesOrderLines_UnitPrice] CHECK ([UnitPrice] >= 0)
)
GO
