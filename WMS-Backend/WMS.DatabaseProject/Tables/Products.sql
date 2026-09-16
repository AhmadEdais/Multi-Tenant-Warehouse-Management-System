CREATE TABLE [dbo].[Products]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [SKU] NVARCHAR(100) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [UnitOfMeasure] NVARCHAR(50) NOT NULL,
    [UnitCost] DECIMAL(18,4) NOT NULL,
    [UnitPrice] DECIMAL(18,4) NOT NULL,
    [ReorderPoint] INT NOT NULL CONSTRAINT [DF_Products_ReorderPoint] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Products_IsActive] DEFAULT (1),
    [RowVersion] ROWVERSION NOT NULL, -- Concurrency token

    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Products_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [UQ_Products_TenantId_SKU] UNIQUE NONCLUSTERED ([TenantId], [SKU])
);
GO

CREATE NONCLUSTERED INDEX [IX_Products_TenantId] ON [dbo].[Products]([TenantId]);
GO