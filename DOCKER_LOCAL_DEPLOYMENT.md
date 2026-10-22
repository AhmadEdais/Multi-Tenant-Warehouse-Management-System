# Local Docker Desktop deployment (Windows)

This stack runs the Angular production build and API behind Caddy at
`https://wms.local.test`. SQL Server 2022 Express runs on the private Compose
network and is reachable from Windows only at `127.0.0.1,14333`. The API is not
published to the host. Caddy retains `/api/v1` when proxying; its fixed Docker
IP is the sole trusted forwarded-header proxy. Docker builds neither the legacy
SSDT project nor a database schema. It does not create logins or administrators.

## Prerequisites and settings

Install Docker Desktop for Windows, enable the WSL 2 backend and Linux
containers, and allocate at least 6 GB memory. SQL Server alone requires at
least 2 GB. Keep Docker Desktop running. Use PowerShell in the repository root.
Ports 80, 443, and 14333 on Windows must be free. Do not use `docker compose
down -v`; the named SQL volume contains the database.

If `.env` does not exist, copy `.env.example` to `.env`. If it already exists,
**edit it in place**; the setup never replaces it. It is gitignored. Fill these
Compose inputs:

| Key in `.env` | Local value / purpose |
| --- | --- |
| `MSSQL_SA_PASSWORD` | New strong SQL `sa` password, used only for initial DBA work and SQL health checks. |
| `WMS_DB_NAME` | `WMS` (use this exact name when publishing the DACPAC). |
| `WMS_DB_PASSWORD` | Different strong password for the manually created `wms_app` login. Avoid semicolons and double quotes because Compose inserts this into a SQL connection string. |
| `JwtSettings__Secret` | Fresh random secret of at least 32 UTF-8 bytes. Keep any final `=` characters. |
| `JwtSettings__Issuer` | A stable nonempty value, for example `WMS`. |
| `JwtSettings__Audience` | A stable nonempty value, for example `WMS`. |
| `Frontend__AllowedOrigins__0` | Exactly `https://wms.local.test`. |
| `AllowedHosts` | Exactly `wms.local.test` (no scheme or port). |
| `Demo__Enabled` | `false` during database setup; change to `true` after verifying login and cleanup. |
| `Demo__CleanupEnabled` | `true` after database setup. Set `false` during initial setup if you want to avoid expected cleanup connection errors. |

Compose sets `ASPNETCORE_ENVIRONMENT=Production`, constructs
`ConnectionStrings__WmsConnection` with `Server=sqlserver,1433`, `WMS_DB_NAME`,
user `wms_app`, and `WMS_DB_PASSWORD`, and sets
`ReverseProxy__TrustedProxies=172.30.44.10`. Existing values for these keys in
`.env` are **not passed** to the API by Compose. `TrustServerCertificate=True`
is used for the private container-to-container SQL connection because local
SQL Server has no CA-issued server certificate. For Visual Studio development
outside Docker, the API continues to use .NET User Secrets; Compose does not
change that workflow.

If a password contains `$`, enclose its `.env` value in single quotes so
Compose treats it literally. If the fixed `172.30.44.0/24` subnet conflicts
with a VPN or another Docker network, choose another private subnet and update
both the Caddy IP and `ReverseProxy__TrustedProxies` in `compose.yaml` together.

The Docker build contexts are `WMS-Backend` and `WMS-Frontend`, so the root
`.env` is outside both image contexts. Never paste passwords into source files,
terminal commands, or issue reports. `docker compose config` expands secrets;
use `docker compose config --quiet` for validation and keep access to Docker
configuration and process environments restricted.

## Windows hostname and HTTPS

1. Open `C:\Windows\System32\drivers\etc\hosts` in Notepad **as
   Administrator**. Add the line `127.0.0.1 wms.local.test` and save.
2. Start Caddy using the commands below. It creates an internal local CA in
   the persistent `caddy_data` volume.
3. In PowerShell, copy and trust Caddy's local root certificate for your
   Windows user:

   ```powershell
   docker compose cp caddy:/data/caddy/pki/authorities/local/root.crt "$env:TEMP\wms-local-root.crt"
   certutil -user -addstore Root "$env:TEMP\wms-local-root.crt"
   ```

4. Open `https://wms.local.test` in a fresh browser window. Use the HTTPS
   origin for login, demo, refresh, and logout. The existing refresh cookie
   stays `Secure`, `HttpOnly`, `SameSite=Strict`, and host-only.

The production Caddyfile is supplied as `WMS-Frontend/Caddyfile.production`
for a later deployment at `wms.ahmadedais.com`. It uses Caddy's normal public
HTTPS certificate management. This local Compose file binds ports to
`127.0.0.1` and uses the local Caddyfile; it does **not** configure DNS or
publish the application publicly. Future VPS deployment needs a separate
Compose override for public 80/443 bindings, the production Caddyfile,
production origins/hosts, and a review of the trusted Caddy IP.

## Build and start

From `C:\VS\WmS` in PowerShell:

```powershell
docker version
docker compose version
docker compose config --quiet
docker compose build api caddy
docker compose up -d sqlserver
docker compose ps
```

Wait until `sqlserver` is healthy. Its probe makes a real `SELECT 1` login
without showing the `sa` password in the command line. Before DACPAC and
`wms_app` creation, starting the API is allowed but `/health/ready` should
return 503; this is expected. Its `/health/live` should still return 200.
To inspect that pre-initialization state through Caddy, run
`docker compose up -d api caddy`, then query both health paths after trusting
the local certificate. The API may log a cleanup connection failure until
the app login is created; it retries at the next UTC hour.

After completing the database steps below:

```powershell
docker compose up -d api caddy
docker compose ps
docker compose logs --tail=100 api
curl.exe -i https://wms.local.test/health/live
curl.exe -i https://wms.local.test/health/ready
curl.exe -I https://wms.local.test/
```

Expected: both health paths return 200 after initialization; `/` serves the
Angular app. The Caddyfile uses `handle /api/v1/*` with `reverse_proxy`, which
passes the full API path to ASP.NET Core. A simple routing check is:

```powershell
curl.exe -i https://wms.local.test/api/v1/Authorization/refresh
```

This GET should reach the controller and return 405 (POST is required), rather
than returning the Angular `index.html`. Caddy forwards the original Host and
its own `X-Forwarded-For` and `X-Forwarded-Proto`; the API accepts those
headers only from Caddy's fixed Docker IP.

## Publish the DACPAC and configure the SQL login

1. Build the legacy SQL project **on Windows with Visual Studio SSDT**. The
   following uses the installed Visual Studio Community MSBuild:

   ```powershell
   & 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' 'WMS-Backend\WMS.DatabaseProject\WMS.DatabaseProject.sqlproj' /t:Build /p:Configuration=Debug /v:minimal
   ```

   The output is `WMS-Backend\WMS.DatabaseProject\bin\Debug\WMS.DatabaseProject.dacpac`.
   Do not build this `.sqlproj` inside the Linux API image or run EF migrations.
2. In Visual Studio's **SQL Server Object Explorer**, connect to SQL Server
   `127.0.0.1,14333` using SQL authentication as `sa` and the private
   `MSSQL_SA_PASSWORD` from `.env`. Trust the container's SQL server
   certificate for this local connection. Open the SQL project, choose
   **Publish**, target database `WMS` (or the exact `WMS_DB_NAME` you set),
   **Generate Script** first, and inspect the target-specific deployment
   script. For a new empty database, confirm it creates the expected tables,
   constraints, and `dbo.CleanupExpiredDemoTenants`, then publish. For an
   existing database, back it up and review any data-loss warnings before
   publishing. Do not enable options that drop data as an automatic shortcut.
3. In a new `sa` query window for the target database, verify the post-deploy
   role seed and schema:

   ```sql
   SELECT Name FROM dbo.Roles ORDER BY Name;
   SELECT COUNT(*) AS RoleCount FROM dbo.Roles;
   SELECT OBJECT_ID(N'dbo.CleanupExpiredDemoTenants', N'P') AS CleanupProcedureId;
   SELECT OBJECT_ID(N'dbo.Users', N'U') AS UsersTableId;
   ```

   Expect exactly five roles: `Analyst`, `SystemAdmin`, `TenantAdmin`,
   `WarehouseManager`, and `WarehouseOperator`; both object IDs must be
   non-null. SQL Server Express can be checked with
   `SELECT SERVERPROPERTY('Edition') AS Edition;`.
4. Still as `sa`, create the dedicated app login in a **private SSMS/Visual
   Studio query window**. Replace the password placeholder in the editor with
   the exact value of `WMS_DB_PASSWORD`, execute, then close the query without
   saving it. Never place the filled query in this repository:

   ```sql
   USE [master];
   CREATE LOGIN [wms_app] WITH PASSWORD = N'<WMS_DB_PASSWORD>', CHECK_POLICY = ON;
   USE [WMS];
   CREATE USER [wms_app] FOR LOGIN [wms_app];
   ALTER ROLE [db_datareader] ADD MEMBER [wms_app];
   ALTER ROLE [db_datawriter] ADD MEMBER [wms_app];
   GRANT EXECUTE ON OBJECT::[dbo].[CleanupExpiredDemoTenants] TO [wms_app];
   ```

   Use the actual database name instead of `[WMS]` if `WMS_DB_NAME` differs.
   The app login gets table reads/writes and only the cleanup procedure's
   execute permission; it gets no server administrator or schema owner role.
   For a previously initialized volume, inspect existing login/user/role
   membership before running `CREATE` again.
5. Start API and Caddy (commands above), then confirm `/health/ready` is 200.
   If it is 503, inspect `docker compose logs --tail=100 api` and verify the
   database name, app login/password, and SQL privileges in a SQL client. A
   successful TCP SQL health check alone does not prove the WMS database or
   app login exists.

## One-time SystemAdmin and authentication

The API image contains a separately published maintenance utility at
`/maintenance/WMS.BootstrapAdmin.dll`. It is never run by API startup. After
the DACPAC and `wms_app` setup, execute it **once** in an interactive terminal:

```powershell
docker compose exec api dotnet /maintenance/WMS.BootstrapAdmin.dll
```

Enter email, full name, password, and confirmation at the prompts. The
password is hidden and must be 16 or more characters and at most 72 UTF-8
bytes. The utility refuses duplicate email or an existing SystemAdmin and
commits the user and role in one transaction. Do not put the password in CLI
arguments, `.env`, or logs.

For browser verification, visit `https://wms.local.test/login`, sign in as
the new SystemAdmin, and check that the platform navigation loads while
tenant operational pages remain unavailable. In browser Developer Tools,
verify:

1. Login `POST /api/v1/Authorization/login` returns an access token and a
   `__Host-wms-refresh` cookie with `Secure`, `HttpOnly`, `SameSite=Strict`,
   and `Path=/`.
2. Reload the page. Angular's session restore calls
   `POST /api/v1/Authorization/refresh`, rotates the cookie, and loads
   `GET /api/v1/users/me`.
3. Logout calls `POST /api/v1/Authorization/logout`, clears the cookie,
   and revokes the session. A subsequent refresh must fail.
4. Try a refresh request from an untrusted `Origin`; the API must reject it
   with 403. Test from the normal page first, where Origin is the trusted
   HTTPS host.

Once this works, set `Demo__Enabled=true` and
`Demo__CleanupEnabled=true` in `.env`, then recreate only the API:

```powershell
docker compose up -d --force-recreate api
```

Test **Try Live Demo** through the same HTTPS origin and monitor API cleanup
logs. The demo creates real SQL rows; cleanup physically removes expired demo
tenants. Use a disposable local database for manual cleanup experiments.

## Persistence and shutdown

Record the role count (and, after bootstrap, the SystemAdmin user), recreate
the SQL container, and query the same rows again in the SQL client:

```powershell
docker compose up -d --force-recreate sqlserver
docker compose ps
```

The named `sql_data` volume remains attached, so the rows should persist.
The Caddy CA and certificates likewise remain in `caddy_data`. To stop and
restart without removing volumes:

```powershell
docker compose down
docker compose up -d
```

Never add `-v` to `down` for this database. For an existing SQL volume, changing
`MSSQL_SA_PASSWORD` in `.env` does not rotate the SQL login password; change it
inside SQL Server deliberately. A Docker volume also does not replace a SQL
backup. Before any future public deployment, arrange backups, restore tests,
disk monitoring, and secure production secret storage.
