CREATE TABLE [dbo].[SalesOrderSequences]
(
    [TenantId] INT NOT NULL,
    [LastNumber] BIGINT NOT NULL,

    CONSTRAINT [PK_SalesOrderSequences] PRIMARY KEY CLUSTERED ([TenantId]),
    CONSTRAINT [FK_SalesOrderSequences_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [CK_SalesOrderSequences_LastNumber] CHECK ([LastNumber] >= 0)
)
GO
