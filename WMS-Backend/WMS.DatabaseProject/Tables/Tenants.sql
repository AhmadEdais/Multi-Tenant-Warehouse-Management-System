CREATE TABLE [dbo].[Tenants] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(20) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Tenants_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Tenants_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByUserId] INT NULL,
    [IsDemo] BIT NOT NULL CONSTRAINT [DF_Tenants_IsDemo] DEFAULT (0),
    [DemoExpiresAtUtc] DATETIME2 NULL,
    CONSTRAINT [CK_Tenants_DemoExpiration] CHECK (
        ([IsDemo] = 0 AND [DemoExpiresAtUtc] IS NULL) OR
        ([IsDemo] = 1 AND [DemoExpiresAtUtc] IS NOT NULL AND [DemoExpiresAtUtc] > [CreatedAtUtc])),
    
    CONSTRAINT [PK_Tenants] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Tenants_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);
GO

-- Immutable membership index used by demo admission. Normal tenant activation/name
-- updates do not lock these index keys. Cleanup must coordinate with WMS:DemoCapacity.
CREATE NONCLUSTERED INDEX [IX_Tenants_DemoCapacity]
ON [dbo].[Tenants] ([IsDemo], [Id]) WHERE [IsDemo] = 1;
GO
