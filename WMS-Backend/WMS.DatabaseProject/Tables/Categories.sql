CREATE TABLE [dbo].[Categories]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [ParentCategoryId] INT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT (1),

    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Categories_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_Categories_Parent] FOREIGN KEY ([ParentCategoryId]) REFERENCES [dbo].[Categories]([Id])
);
GO

-- Indexes to optimize tenant lookups and the recursive CTE query
CREATE NONCLUSTERED INDEX [IX_Categories_TenantId] ON [dbo].[Categories]([TenantId]);
GO
CREATE NONCLUSTERED INDEX [IX_Categories_ParentCategoryId] ON [dbo].[Categories]([ParentCategoryId]);
GO