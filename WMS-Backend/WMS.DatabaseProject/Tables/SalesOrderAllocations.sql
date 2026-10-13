CREATE TABLE [dbo].[SalesOrderAllocations]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SalesOrderLineId] BIGINT NOT NULL,
    [StockLevelId] INT NOT NULL,
    [Quantity] DECIMAL(18,2) NOT NULL,
    [Status] TINYINT NOT NULL CONSTRAINT [DF_SalesOrderAllocations_Status] DEFAULT (1), -- 1=Active, 2=Released, 3=Shipped
    [CreatedOnUtc] DATETIME2 NOT NULL CONSTRAINT [DF_SalesOrderAllocations_CreatedOnUtc] DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(128) NOT NULL,
    [ReleasedOnUtc] DATETIME2 NULL,
    [ReleasedBy] NVARCHAR(128) NULL,
    [ReleaseReason] NVARCHAR(200) NULL,

    CONSTRAINT [PK_SalesOrderAllocations] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_SalesOrderAllocations_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_SalesOrderAllocations_Lines] FOREIGN KEY ([SalesOrderLineId]) REFERENCES [dbo].[SalesOrderLines]([Id]),
    CONSTRAINT [FK_SalesOrderAllocations_StockLevels] FOREIGN KEY ([StockLevelId]) REFERENCES [dbo].[StockLevels]([Id]),
    CONSTRAINT [UQ_SalesOrderAllocations_Line_StockLevel] UNIQUE ([SalesOrderLineId], [StockLevelId]),
    CONSTRAINT [CHK_SalesOrderAllocations_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CHK_SalesOrderAllocations_Status] CHECK ([Status] IN (1, 2, 3)),
    CONSTRAINT [CHK_SalesOrderAllocations_ReleaseMetadata] CHECK (
        ([Status] = 1 AND [ReleasedOnUtc] IS NULL AND [ReleasedBy] IS NULL AND [ReleaseReason] IS NULL)
        OR ([Status] = 2 AND [ReleasedOnUtc] IS NOT NULL
            AND [ReleasedBy] IS NOT NULL AND LEN(LTRIM(RTRIM([ReleasedBy]))) > 0
            AND [ReleaseReason] IS NOT NULL AND LEN(LTRIM(RTRIM([ReleaseReason]))) > 0)
        OR [Status] = 3)
)
GO

CREATE NONCLUSTERED INDEX [IX_SalesOrderAllocations_StockLevelId]
ON [dbo].[SalesOrderAllocations] ([StockLevelId])
GO
