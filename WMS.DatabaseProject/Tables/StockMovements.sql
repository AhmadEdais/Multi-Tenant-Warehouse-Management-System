CREATE TABLE [dbo].[StockMovements]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [LocationId] INT NOT NULL,
    
    -- Can be positive (Receipt/TransferIn) or negative (Shipment/TransferOut)
    [Quantity] DECIMAL(18,2) NOT NULL, 
    
    -- Enum: 1=Receipt, 2=Shipment, 3=Adjustment, 4=TransferIn, 5=TransferOut
    [MovementType] TINYINT NOT NULL, 
    
    -- Polymorphic relationship (Links to POs, Sales Orders, etc.)
    [ReferenceTable] NVARCHAR(50) NULL,
    [ReferenceId] INT NULL,

    -- Immutable Audit Columns (Who did it and exactly when)
    [CreatedOnUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(128) NOT NULL,

    CONSTRAINT [PK_StockMovements] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_StockMovements_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_StockMovements_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
    CONSTRAINT [FK_StockMovements_Locations] FOREIGN KEY ([LocationId]) REFERENCES [dbo].[Locations]([Id])
)
GO

-- Index to quickly pull the history of a specific product for auditors
CREATE NONCLUSTERED INDEX [IX_StockMovements_ProductId_CreatedOn] ON [dbo].[StockMovements] ([ProductId], [CreatedOnUtc] DESC)
GO