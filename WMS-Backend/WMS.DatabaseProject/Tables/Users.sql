CREATE TABLE [dbo].[Users]
(
    [Id] INT NOT NULL IDENTITY(1,1),
    [TenantId] INT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [FullName] NVARCHAR(200) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
    [CreatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Users_CreatedAtUtc] DEFAULT (GETUTCDATE()),
    [LastLoginAtUtc] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Users_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]),
    CONSTRAINT [UQ_Users_TenantId_Email] UNIQUE NONCLUSTERED ([TenantId] ASC, [Email] ASC)
);