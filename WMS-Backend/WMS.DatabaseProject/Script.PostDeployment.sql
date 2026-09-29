/*
--------------------------------------------------------------------------------------
This file contains SQL statements that will be appended to the build script.
--------------------------------------------------------------------------------------
*/

PRINT 'Seeding [dbo].[Roles]...';

-- Stop before removing unsupported roles if users still depend on them.
-- An administrator must review and replace those assignments with supported roles.
IF EXISTS (
    SELECT 1
    FROM [dbo].[UserRoles] AS ur
    INNER JOIN [dbo].[Roles] AS r ON r.[Id] = ur.[RoleId]
    WHERE r.[Name] NOT IN (
        'SystemAdmin', 'TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst'
    )
)
BEGIN
    THROW 51000, 'Unsupported role assignments exist. Reassign affected users to supported roles before deploying.', 1;
END;

MERGE INTO [dbo].[Roles] AS Target
USING (VALUES
    ('SystemAdmin', 'Platform tenant administration and system health; no tenant warehouse data access.'),
    ('TenantAdmin', 'Administrator for a specific tenant workspace.'),
    ('WarehouseManager', 'Manages tenant operations; warehouse settings require assignment scope.'),
    ('WarehouseOperator', 'Executes day-to-day warehouse operations.'),
    ('Analyst', 'Read-only access to warehouse reports and data.')
) AS Source ([Name], [Description])
ON (Target.[Name] = Source.[Name])
WHEN MATCHED AND ISNULL(Target.[Description], '') <> Source.[Description] THEN
    UPDATE SET [Description] = Source.[Description]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Name], [Description])
    VALUES (Source.[Name], Source.[Description]);

-- The guard above ensures these rows have no user assignments before deletion.
DELETE FROM [dbo].[Roles]
WHERE [Name] NOT IN ('SystemAdmin', 'TenantAdmin', 'WarehouseManager', 'WarehouseOperator', 'Analyst');

PRINT 'Finished seeding [dbo].[Roles].';

-- No SystemAdmin user is seeded here. Bootstrap that account through a controlled
-- deployment process, resolving its role by name rather than by an identity value.
