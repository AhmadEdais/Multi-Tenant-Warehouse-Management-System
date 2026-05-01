/*
--------------------------------------------------------------------------------------
This file contains SQL statements that will be appended to the build script.
--------------------------------------------------------------------------------------
*/

PRINT 'Seeding [dbo].[Roles]...';

MERGE INTO [dbo].[Roles] AS Target
USING (VALUES
    ('SystemAdmin', 'Global system administrator with access across all tenants.'),
    ('TenantAdmin', 'Administrator for a specific tenant workspace.'),
    ('WarehouseManager', 'Full control over operations within a specific warehouse.'),
    ('InventoryOperator', 'Executes day-to-day inbound, outbound, and movement tasks.'),
    ('Auditor', 'Read-only access for compliance and stock level verification.')
) AS Source ([Name], [Description])
ON (Target.[Name] = Source.[Name])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Name], [Description])
    VALUES (Source.[Name], Source.[Description]);

PRINT 'Finished seeding [dbo].[Roles].';

PRINT 'Seeding [dbo].[UserRoles] (Admin Assignment)...';

-- We use MERGE to ensure we don't create duplicate assignments if we run this twice.
MERGE INTO [dbo].[UserRoles] AS Target
USING (VALUES
    (1, 1, 1, GETUTCDATE()) -- UserId: 1, RoleId: 1, AssignedBy: 1, AssignedAt: Now
) AS Source ([UserId], [RoleId], [AssignedByUserId], [AssignedAtUtc])
ON (Target.[UserId] = Source.[UserId] AND Target.[RoleId] = Source.[RoleId])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserId], [RoleId], [AssignedByUserId], [AssignedAtUtc])
    VALUES (Source.[UserId], Source.[RoleId], Source.[AssignedByUserId], Source.[AssignedAtUtc]);

PRINT 'Finished seeding [dbo].[UserRoles].';
GO
