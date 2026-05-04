CREATE TABLE [dbo].[Customers]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [ContactEmail] NVARCHAR(256) NULL,
    [PhoneNumber] NVARCHAR(50) NOT NULL,
    [Address] NVARCHAR(500) NULL,
    [CreditLimit] DECIMAL(18,2) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Customers_IsActive] DEFAULT 1,
    
    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Customers_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Customers_Tenant_Code] UNIQUE NONCLUSTERED ([TenantId], [Code])
);
GO