CREATE TABLE [dbo].[StockLevels]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [LocationId] INT NOT NULL,
    
    [QuantityOnHand] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [QuantityAllocated] DECIMAL(18,2) NOT NULL DEFAULT 0,
    
    -- Concurrency Token for EF Core
    [RowVersion] ROWVERSION NOT NULL,

    -- Standard Audit Columns
    [CreatedOnUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(128) NOT NULL,
    [LastModifiedOnUtc] DATETIME2 NULL,
    [LastModifiedBy] NVARCHAR(128) NULL,

    CONSTRAINT [PK_StockLevels] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_StockLevels_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_StockLevels_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
    CONSTRAINT [FK_StockLevels_Locations] FOREIGN KEY ([LocationId]) REFERENCES [dbo].[Locations]([Id]),
    
    -- Critical Data Integrity Constraints
    CONSTRAINT [CHK_StockLevels_QuantityOnHand] CHECK ([QuantityOnHand] >= 0),
    CONSTRAINT [CHK_StockLevels_QuantityAllocated] CHECK ([QuantityAllocated] >= 0),
    
    -- A product can only have ONE level record per physical bin
    CONSTRAINT [UQ_StockLevels_Tenant_Product_Location] UNIQUE ([TenantId], [ProductId], [LocationId])
)
GO

-- Index to quickly find all stock for a specific product
CREATE NONCLUSTERED INDEX [IX_StockLevels_ProductId] ON [dbo].[StockLevels] ([ProductId])
GO