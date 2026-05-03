CREATE TABLE [dbo].[ProductCategories]
(
    [ProductId] INT NOT NULL,
    [CategoryId] INT NOT NULL,

    CONSTRAINT [PK_ProductCategories] PRIMARY KEY CLUSTERED ([ProductId], [CategoryId]),
    CONSTRAINT [FK_ProductCategories_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductCategories_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE CASCADE
);
GO

-- Index the CategoryId to optimize finding all products in a specific category
CREATE NONCLUSTERED INDEX [IX_ProductCategories_CategoryId] ON [dbo].[ProductCategories]([CategoryId]);
GO