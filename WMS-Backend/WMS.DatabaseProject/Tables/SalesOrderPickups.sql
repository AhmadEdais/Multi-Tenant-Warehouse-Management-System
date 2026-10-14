CREATE TABLE [dbo].[SalesOrderPickups]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SalesOrderId] INT NOT NULL,
    [PickedOnUtc] DATETIME2 NOT NULL,
    [PickedBy] NVARCHAR(128) NOT NULL,
    [ReceivedBy] NVARCHAR(200) NULL,
    [Notes] NVARCHAR(500) NULL,

    CONSTRAINT [PK_SalesOrderPickups] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SalesOrderPickups_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_SalesOrderPickups_SalesOrders] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders]([Id])
)
GO

CREATE NONCLUSTERED INDEX [IX_SalesOrderPickups_SalesOrder_PickedOn]
ON [dbo].[SalesOrderPickups] ([SalesOrderId], [PickedOnUtc] DESC, [Id] DESC)
GO
