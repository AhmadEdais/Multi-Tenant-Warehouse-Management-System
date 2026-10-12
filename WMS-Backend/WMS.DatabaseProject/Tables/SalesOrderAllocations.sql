CREATE TABLE [dbo].[SalesOrderAllocations]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SalesOrderLineId] BIGINT NOT NULL,
    [StockLevelId] INT NOT NULL,
    [Quantity] DECIMAL(18,2) NOT NULL,
    [CreatedOnUtc] DATETIME2 NOT NULL CONSTRAINT [DF_SalesOrderAllocations_CreatedOnUtc] DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(128) NOT NULL,

    CONSTRAINT [PK_SalesOrderAllocations] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SalesOrderAllocations_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_SalesOrderAllocations_Lines] FOREIGN KEY ([SalesOrderLineId]) REFERENCES [dbo].[SalesOrderLines]([Id]),
    CONSTRAINT [FK_SalesOrderAllocations_StockLevels] FOREIGN KEY ([StockLevelId]) REFERENCES [dbo].[StockLevels]([Id]),
    CONSTRAINT [UQ_SalesOrderAllocations_Line_StockLevel] UNIQUE ([SalesOrderLineId], [StockLevelId]),
    CONSTRAINT [CHK_SalesOrderAllocations_Quantity] CHECK ([Quantity] > 0)
)
GO

CREATE NONCLUSTERED INDEX [IX_SalesOrderAllocations_StockLevelId]
ON [dbo].[SalesOrderAllocations] ([StockLevelId])
GO
