CREATE TABLE [dbo].[PurchaseOrderLines]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [PurchaseOrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    
    [ExpectedQuantity] DECIMAL(18,2) NOT NULL,
    [ReceivedQuantity] DECIMAL(18,2) NOT NULL DEFAULT 0,

    CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PurchaseOrderLines_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[PurchaseOrders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PurchaseOrderLines_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
    
    -- You can't order zero items
    CONSTRAINT [CHK_POLines_ExpectedQuantity] CHECK ([ExpectedQuantity] > 0),
    
    -- You can't receive negative items, AND you can't receive more than you ordered!
    CONSTRAINT [CHK_POLines_ReceivedQuantity] CHECK ([ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [ExpectedQuantity]),
    
    -- A product should only appear once per Purchase Order
    CONSTRAINT [UQ_PurchaseOrderLines_PO_Product] UNIQUE ([PurchaseOrderId], [ProductId])
)
GO