CREATE TABLE [dbo].[SalesOrderPickupLines]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SalesOrderPickupId] BIGINT NOT NULL,
    [SalesOrderAllocationId] BIGINT NOT NULL,
    [Quantity] DECIMAL(18,2) NOT NULL,

    CONSTRAINT [PK_SalesOrderPickupLines] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SalesOrderPickupLines_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_SalesOrderPickupLines_Pickups] FOREIGN KEY ([SalesOrderPickupId]) REFERENCES [dbo].[SalesOrderPickups]([Id]),
    CONSTRAINT [FK_SalesOrderPickupLines_Allocations] FOREIGN KEY ([SalesOrderAllocationId]) REFERENCES [dbo].[SalesOrderAllocations]([Id]),
    CONSTRAINT [UQ_SalesOrderPickupLines_Pickup_Allocation] UNIQUE ([SalesOrderPickupId], [SalesOrderAllocationId]),
    CONSTRAINT [CHK_SalesOrderPickupLines_Quantity] CHECK ([Quantity] > 0)
)
GO

CREATE NONCLUSTERED INDEX [IX_SalesOrderPickupLines_AllocationId]
ON [dbo].[SalesOrderPickupLines] ([SalesOrderAllocationId])
GO
