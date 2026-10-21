CREATE TABLE [dbo].[AuthSessions]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [LastRefreshedAtUtc] DATETIME2 NULL,
    [IdleExpiresAtUtc] DATETIME2 NOT NULL,
    [AbsoluteExpiresAtUtc] DATETIME2 NOT NULL,
    [RememberMe] BIT NOT NULL,
    [RevokedAtUtc] DATETIME2 NULL,
    [RevocationReason] NVARCHAR(100) NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_AuthSessions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_AuthSessions_Users] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
)
GO

CREATE NONCLUSTERED INDEX [IX_AuthSessions_UserId]
ON [dbo].[AuthSessions] ([UserId])
GO
