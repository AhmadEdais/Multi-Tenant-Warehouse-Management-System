CREATE PROCEDURE [dbo].[CleanupExpiredDemoTenants]
    @BatchSize INT,
    @AfterTenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET LOCK_TIMEOUT 5000;

    IF @BatchSize IS NULL OR @BatchSize NOT BETWEEN 1 AND 100
        OR @AfterTenantId IS NULL OR @AfterTenantId < 0
        THROW 50001, 'Invalid demo cleanup batch arguments.', 1;

    -- The caller advances by Id, including failed/skipped candidates, so one bad
    -- tenant cannot be selected repeatedly in the same hourly execution.
    DECLARE @Candidates TABLE (Id INT NOT NULL PRIMARY KEY);
    INSERT INTO @Candidates (Id)
    SELECT TOP (@BatchSize) Id
    FROM dbo.Tenants
    WHERE Id > @AfterTenantId AND IsDemo = 1
        AND DemoExpiresAtUtc IS NOT NULL
        AND DemoExpiresAtUtc <= SYSUTCDATETIME()
    ORDER BY Id;

    DECLARE @Results TABLE
    (
        TenantId INT NOT NULL PRIMARY KEY,
        ResultCode INT NOT NULL, -- 1 deleted, 0 already absent/ineligible, -1 failed
        ErrorNumber INT NULL,
        ErrorMessage NVARCHAR(2048) NULL
    );
    DECLARE @TenantId INT;
    DECLARE @LockResult INT;
    DECLARE @Deleted BIT;
    DECLARE @Affected INT;

    DECLARE candidate_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM @Candidates ORDER BY Id;
    OPEN candidate_cursor;
    FETCH NEXT FROM candidate_cursor INTO @TenantId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Deleted = 0;
        BEGIN TRY
            BEGIN TRANSACTION;
            -- The same transaction-owned resource is used by demo admission.
            -- Only one small tenant deletion or final admission check runs at once.
            EXEC @LockResult = sys.sp_getapplock
                @Resource = N'WMS:DemoCapacity', @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 5000;
            IF @LockResult < 0
                THROW 50002, 'Demo cleanup capacity lock is busy.', 1;

            -- Recheck under the lock: another API instance may have removed it.
            IF EXISTS (SELECT 1 FROM dbo.Tenants WITH (UPDLOCK, HOLDLOCK)
                       WHERE Id = @TenantId AND IsDemo = 1
                         AND DemoExpiresAtUtc IS NOT NULL
                         AND DemoExpiresAtUtc <= SYSUTCDATETIME())
            BEGIN
                -- These are the only cascading paths from tenant-owned parents to
                -- child rows without their own tenant FK. Fail closed on bad links.
                IF EXISTS (SELECT 1 FROM dbo.ProductCategories pc
                           JOIN dbo.Products p ON p.Id = pc.ProductId
                           JOIN dbo.Categories c ON c.Id = pc.CategoryId
                           WHERE (p.TenantId = @TenantId AND c.TenantId <> @TenantId)
                              OR (c.TenantId = @TenantId AND p.TenantId <> @TenantId))
                    THROW 50003, 'Cross-tenant product/category link blocks cleanup.', 1;
                IF EXISTS (SELECT 1 FROM dbo.PurchaseOrderLines l
                           JOIN dbo.PurchaseOrders o ON o.Id = l.PurchaseOrderId
                           WHERE o.TenantId = @TenantId AND l.TenantId <> @TenantId)
                    THROW 50004, 'Cross-tenant purchase order line blocks cleanup.', 1;
                IF EXISTS (SELECT 1 FROM dbo.SalesOrderLines l
                           JOIN dbo.SalesOrders o ON o.Id = l.SalesOrderId
                           WHERE o.TenantId = @TenantId AND l.TenantId <> @TenantId)
                    THROW 50005, 'Cross-tenant sales order line blocks cleanup.', 1;

                -- RefreshTokens has no TenantId and points to itself. Detach only
                -- this tenant's replacement links before deleting its token chain.
                UPDATE rt SET ReplacedByTokenId = NULL
                FROM dbo.RefreshTokens rt
                JOIN dbo.AuthSessions s ON s.Id = rt.AuthSessionId
                JOIN dbo.Users u ON u.Id = s.UserId
                WHERE u.TenantId = @TenantId AND rt.ReplacedByTokenId IS NOT NULL;
                DELETE rt FROM dbo.RefreshTokens rt
                JOIN dbo.AuthSessions s ON s.Id = rt.AuthSessionId
                JOIN dbo.Users u ON u.Id = s.UserId
                WHERE u.TenantId = @TenantId;
                DELETE s FROM dbo.AuthSessions s
                JOIN dbo.Users u ON u.Id = s.UserId WHERE u.TenantId = @TenantId;

                DELETE FROM dbo.StockMovements WHERE TenantId = @TenantId;
                DELETE FROM dbo.SalesOrderPickupLines WHERE TenantId = @TenantId;
                DELETE FROM dbo.SalesOrderPickups WHERE TenantId = @TenantId;
                DELETE FROM dbo.SalesOrderAllocations WHERE TenantId = @TenantId;
                DELETE FROM dbo.SalesOrderLines WHERE TenantId = @TenantId;
                DELETE FROM dbo.SalesOrders WHERE TenantId = @TenantId;
                DELETE FROM dbo.PurchaseOrderLines WHERE TenantId = @TenantId;
                DELETE FROM dbo.PurchaseOrders WHERE TenantId = @TenantId;
                DELETE FROM dbo.StockLevels WHERE TenantId = @TenantId;

                DELETE pc FROM dbo.ProductCategories pc
                JOIN dbo.Products p ON p.Id = pc.ProductId
                JOIN dbo.Categories c ON c.Id = pc.CategoryId
                WHERE p.TenantId = @TenantId AND c.TenantId = @TenantId;
                DELETE FROM dbo.Products WHERE TenantId = @TenantId;

                -- Self-referencing trees must be removed from leaves to roots.
                WHILE EXISTS (SELECT 1 FROM dbo.Categories WHERE TenantId = @TenantId)
                BEGIN
                    DELETE parent FROM dbo.Categories parent
                    WHERE parent.TenantId = @TenantId
                        AND NOT EXISTS (SELECT 1 FROM dbo.Categories child
                                        WHERE child.ParentCategoryId = parent.Id);
                    SET @Affected = @@ROWCOUNT;
                    IF @Affected = 0 THROW 50006, 'Category hierarchy blocks cleanup.', 1;
                END
                WHILE EXISTS (SELECT 1 FROM dbo.Locations WHERE TenantId = @TenantId)
                BEGIN
                    DELETE parent FROM dbo.Locations parent
                    WHERE parent.TenantId = @TenantId
                        AND NOT EXISTS (SELECT 1 FROM dbo.Locations child
                                        WHERE child.ParentLocationId = parent.Id);
                    SET @Affected = @@ROWCOUNT;
                    IF @Affected = 0 THROW 50007, 'Location hierarchy blocks cleanup.', 1;
                END

                DELETE FROM dbo.PurchaseOrderSequences WHERE TenantId = @TenantId;
                DELETE FROM dbo.SalesOrderSequences WHERE TenantId = @TenantId;
                DELETE FROM dbo.Suppliers WHERE TenantId = @TenantId;
                DELETE FROM dbo.Customers WHERE TenantId = @TenantId;
                DELETE FROM dbo.Warehouses WHERE TenantId = @TenantId;
                DELETE ur FROM dbo.UserRoles ur
                JOIN dbo.Users u ON u.Id = ur.UserId WHERE u.TenantId = @TenantId;
                DELETE FROM dbo.Users WHERE TenantId = @TenantId;
                DELETE FROM dbo.Tenants WHERE Id = @TenantId AND IsDemo = 1
                    AND DemoExpiresAtUtc <= SYSUTCDATETIME();
                IF @@ROWCOUNT <> 1 THROW 50008, 'Demo tenant changed during cleanup.', 1;
                SET @Deleted = 1;
            END
            COMMIT TRANSACTION;
            INSERT INTO @Results (TenantId, ResultCode) VALUES (@TenantId, CONVERT(INT, @Deleted));
        END TRY
        BEGIN CATCH
            DECLARE @ErrorNumber INT = ERROR_NUMBER();
            DECLARE @ErrorMessage NVARCHAR(2048) = ERROR_MESSAGE();
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            INSERT INTO @Results (TenantId, ResultCode, ErrorNumber, ErrorMessage)
            VALUES (@TenantId, -1, @ErrorNumber, @ErrorMessage);
        END CATCH;
        FETCH NEXT FROM candidate_cursor INTO @TenantId;
    END
    CLOSE candidate_cursor;
    DEALLOCATE candidate_cursor;

    SELECT TenantId, ResultCode, ErrorNumber, ErrorMessage
    FROM @Results ORDER BY TenantId;
END
GO
