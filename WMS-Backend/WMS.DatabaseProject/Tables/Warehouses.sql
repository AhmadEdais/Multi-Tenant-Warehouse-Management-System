CREATE TABLE [dbo].[Warehouses] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [Code] NVARCHAR(20) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Address] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouses_IsActive] DEFAULT (1),
    
    [CreatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Warehouses_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    -- Used by EF Core for Optimistic Concurrency
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_Warehouses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Warehouses_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]),
    CONSTRAINT [UQ_Warehouses_Tenant_Code] UNIQUE NONCLUSTERED ([TenantId] ASC, [Code] ASC)
);