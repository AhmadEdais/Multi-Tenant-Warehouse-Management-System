CREATE TABLE [dbo].[PurchaseOrderSequences]
(
    [TenantId] INT NOT NULL,
    [LastNumber] BIGINT NOT NULL,

    CONSTRAINT [PK_PurchaseOrderSequences] PRIMARY KEY CLUSTERED ([TenantId] ASC),
    CONSTRAINT [FK_PurchaseOrderSequences_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [CK_PurchaseOrderSequences_LastNumber] CHECK ([LastNumber] >= 0)
)
GO
