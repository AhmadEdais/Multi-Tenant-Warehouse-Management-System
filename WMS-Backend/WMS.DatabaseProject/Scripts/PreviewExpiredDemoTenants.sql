-- Read-only preview. Run against the intended database before manual cleanup.
SELECT Id, Code, CreatedAtUtc, DemoExpiresAtUtc
FROM dbo.Tenants
WHERE IsDemo = 1
  AND DemoExpiresAtUtc IS NOT NULL
  AND DemoExpiresAtUtc <= SYSUTCDATETIME()
ORDER BY Id;
