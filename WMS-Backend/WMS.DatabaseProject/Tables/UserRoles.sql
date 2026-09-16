CREATE TABLE [dbo].[UserRoles]
(
    [UserId] INT NOT NULL,
    [RoleId] INT NOT NULL,
    [AssignedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_UserRoles_AssignedAtUtc] DEFAULT (GETUTCDATE()),
    [AssignedByUserId] INT NULL,
    
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_AssignedByUsers] FOREIGN KEY ([AssignedByUserId]) REFERENCES [dbo].[Users] ([Id])
);