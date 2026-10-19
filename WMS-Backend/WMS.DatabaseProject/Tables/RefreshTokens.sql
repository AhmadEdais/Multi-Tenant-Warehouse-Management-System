CREATE TABLE [dbo].[RefreshTokens]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [AuthSessionId] INT NOT NULL,
    [TokenHash] BINARY(32) NOT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [ConsumedAtUtc] DATETIME2 NULL,
    [ReplacedByTokenId] INT NULL,
    [ReuseDetectedAtUtc] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_RefreshTokens_AuthSessions] FOREIGN KEY ([AuthSessionId])
        REFERENCES [dbo].[AuthSessions]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RefreshTokens_ReplacementToken] FOREIGN KEY ([ReplacedByTokenId])
        REFERENCES [dbo].[RefreshTokens]([Id]) ON DELETE NO ACTION
)
GO

CREATE NONCLUSTERED INDEX [IX_RefreshTokens_AuthSessionId]
ON [dbo].[RefreshTokens] ([AuthSessionId])
GO

CREATE NONCLUSTERED INDEX [IX_RefreshTokens_ReplacedByTokenId]
ON [dbo].[RefreshTokens] ([ReplacedByTokenId])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_RefreshTokens_TokenHash]
ON [dbo].[RefreshTokens] ([TokenHash])
GO
