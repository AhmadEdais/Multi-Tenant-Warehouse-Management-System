# Multi-Tenant Warehouse Management System

A full-stack Warehouse Management System for companies that operate one or more warehouses.

The system is designed around the day-to-day flow of warehouse operations: managing warehouses and locations, maintaining products and business partners, receiving stock from suppliers, tracking inventory, and fulfilling customer orders. Each tenant company works inside its own isolated workspace and should never see another tenant's operational data.

The repository contains both the ASP.NET Core backend and the Angular frontend.

## What the system is for

The project models the main activities that happen inside a warehouse:

- receiving goods from suppliers against Purchase Orders
- storing stock in warehouse locations
- viewing stock levels and stock movement history
- managing products, categories, suppliers, and customers
- reserving and shipping stock for customer orders
- notifying users when stock falls below a reorder point
- keeping tenant data separated from other companies using the system

The business requirements define five user roles:

- **System Administrator** — manages tenant companies at the platform level
- **Tenant Administrator** — manages users, warehouses, master data, and operations inside a tenant
- **Warehouse Manager** — manages warehouse operations such as purchasing, stock, suppliers, customers, and orders
- **Warehouse Operator** — performs operational work such as receiving, picking, packing, and shipping
- **Analyst** — read-only access to inventory, movements, orders, audit information, and alerts

## Main workflows

### Inbound receiving

A warehouse manager creates and approves a Purchase Order for a supplier. When the goods arrive, a warehouse operator receives the accepted quantity into a warehouse location.

Receiving updates the related Purchase Order line, increases stock, and records a Stock Movement so the inventory history can be traced later. Partial receipts are supported by the business requirements.

### Inventory

Inventory is stored per product and location. The system separates:

- **On Hand** — the physical quantity currently stored
- **Allocated** — quantity reserved for customer orders
- **Available** — calculated as `On Hand - Allocated`

Stock movements provide the history behind inventory changes such as receipts, shipments, transfers, and adjustments.

### Outbound fulfillment

The planned outbound workflow starts with a Sales Order. After approval, the system reserves available stock from warehouse locations. Allocation is intended to be all-or-nothing for the whole Sales Order so an order is not left partially reserved.

After allocation, warehouse operators can pick, pack, and finally ship the order. Shipment reduces both On Hand and Allocated quantities and records the corresponding Stock Movements.

### Low-stock monitoring

The requirements include low-stock notifications for users who need to act on them. The planned design combines event-driven updates with a periodic background check so low-stock conditions can be surfaced to the frontend.

## Full-stack architecture

The repository is split into two main applications:

```text
Multi-Tenant-Warehouse-Management-System/
├── WMS-Backend/
│   ├── WMS.API/
│   ├── WMS.Application/
│   ├── WMS.Domain/
│   ├── WMS.Infrastructure/
│   ├── WMS.DatabaseProject/
│   └── WMS.sln
│
├── WMS-Frontend/
│   ├── src/
│   ├── angular.json
│   ├── package.json
│   └── ...
│
├── .gitignore
└── README.md
```

### Backend

The backend uses **ASP.NET Core 8** and is divided into four projects:

- **WMS.Domain** — domain entities and domain rules
- **WMS.Application** — application features, commands, queries, validation, and interfaces
- **WMS.Infrastructure** — EF Core, SQL Server access, authentication implementation, and external infrastructure concerns
- **WMS.API** — HTTP endpoints, authorization policies, exception handling, Swagger, and application startup

The Application layer is organized by feature. Current feature areas include Authorization, Tenants, Users, Warehouses, Locations, Products, Categories, Suppliers, Customers, Inventory, and Inbound operations.

Commands and queries are dispatched with MediatR, while FluentValidation is used for request validation.

### Frontend

The frontend is an **Angular 22** application styled with **Tailwind CSS 4**.

The current frontend foundation includes:

- public landing page
- reusable public, authentication, and application layouts
- login and signup routes
- Reactive Forms for authentication UI
- JWT login integration with the ASP.NET Core API
- token persistence using local or session storage
- HTTP interceptor for attaching the bearer token
- authentication and guest route guards
- authenticated dashboard route
- not-found page

Business feature screens are being added incrementally as the frontend is connected to the existing backend modules.

## Database approach

The database schema is maintained through a SQL Server Database Project (`WMS.DatabaseProject`). EF Core migrations are not used as the source of the schema.

The `.sqlproj` contains the table definitions and database constraints, while EF Core entities are mapped to that schema with Fluent API configuration in the Infrastructure project.

The current database project includes tables for:

- tenants and users
- roles and user-role assignments
- warehouses and locations
- products and categories
- suppliers and customers
- stock levels and stock movements
- purchase orders and purchase order lines

Outbound tables such as Sales Orders and allocations are part of the planned workflow and will be added as that part of the system is implemented.

## Multi-tenancy

Each customer company is represented as a tenant. Operational records belong to a tenant, and the backend applies tenant-aware filtering so requests operate only on data belonging to the current tenant.

This applies to warehouse data such as products, warehouses, locations, suppliers, customers, and inventory.

## Authentication and authorization

The API uses JWT Bearer authentication.

The Angular frontend logs in against the API, stores the returned JWT according to the user's "Remember me" choice, and attaches it to protected API requests through an HTTP interceptor.

Backend authorization policies restrict actions by role. UI route guards protect authenticated frontend routes, but access control is still enforced by the API.

## Tech stack

### Frontend

- Angular 22
- TypeScript 6
- Tailwind CSS 4
- RxJS
- Angular Router
- Angular Reactive Forms

### Backend

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- MediatR 14
- FluentValidation 12
- JWT Bearer Authentication
- BCrypt password hashing
- Swagger / OpenAPI

### Database

- SQL Server
- SQL Server Database Project (`.sqlproj`)
- EF Core Fluent API mappings

## Current development status

The project is being developed feature by feature.

The backend already contains the core tenant, user, warehouse, catalog, partner, inventory, and inbound feature areas. The Angular frontend has been added to the same repository and currently has the application shell and authentication flow in place.

The next stages are to connect the remaining backend modules to Angular screens and complete the unfinished warehouse workflows, including outbound fulfillment and the operational dashboard.

## Production API configuration

Use [`.env.example`](.env.example) as a list of API environment variables. For Windows Docker Desktop, follow [the local Docker deployment guide](DOCKER_LOCAL_DEPLOYMENT.md). The root `.env` supplies Compose interpolation, and `compose.yaml` explicitly passes the required values into the API container. For a later VPS deployment, keep populated secrets in a private location outside the repository and Docker build context. Do not commit the populated file or put credentials in `appsettings.json`, a Dockerfile, or a Compose file.

- `ConnectionStrings__WmsConnection`: SQL Server connection string with the actual server, database, application user, and password. For example, use the shape `Server=<SQL_HOST>,1433;Database=<DB_NAME>;User Id=<DB_USER>;Password=<DB_PASSWORD>;Encrypt=True`, adjusting TLS settings for the SQL Server certificate.
- `JwtSettings__Secret`: a randomly generated signing key of at least 32 UTF-8 bytes. Keep the complete generated value, including any trailing `=` characters.
- `JwtSettings__Issuer` and `JwtSettings__Audience`: values used when issuing and validating WMS tokens.
- `Frontend__AllowedOrigins__0`: the frontend's full HTTPS origin, without a path or trailing slash. Add `__1`, `__2`, and so on only for additional approved origins.
- `AllowedHosts`: the hostname accepted by the API, without a scheme. Separate multiple hostnames with semicolons. Production startup rejects `*`.
- `ReverseProxy__TrustedProxies`: the literal IP address of Caddy as seen by the API container; separate multiple trusted proxy IPs with commas. Do not use a public client IP or a whole Docker subnet.
- `Demo__Enabled`: set to `true` only when the live demo is ready for visitors; keep `Demo__CleanupEnabled=true` when demo is enabled.

Compose's project `.env` file supplies values for interpolation; it does not automatically pass every variable into the API container. The API uses ASP.NET Core's normal environment-variable configuration, where `__` maps to `:`. Production startup checks required settings before accepting requests. `/health/ready` separately checks whether SQL Server is reachable.

For local Visual Studio development, keep `ASPNETCORE_ENVIRONMENT=Development` and put `ConnectionStrings:WmsConnection` and `JwtSettings:Secret` in the **WMS.API** project's .NET User Secrets. The project already has a `UserSecretsId`; no local credentials belong in tracked settings. Production validation does not run in Development.

## Initial SystemAdmin bootstrap

Run the standalone `WMS.BootstrapAdmin` utility **once**, after publishing the WMS DACPAC to the production database. The DACPAC post-deployment script must have seeded the `SystemAdmin` role. The utility is not an API endpoint and is never called during API startup.

1. Publish the utility, then copy the complete published directory to `/opt/wms/bootstrap-admin` on the VPS:

   ```sh
   dotnet publish WMS-Backend/WMS.BootstrapAdmin/WMS.BootstrapAdmin.csproj -c Release -o artifacts/bootstrap-admin
   ```

2. On the VPS, create a private `/etc/wms/bootstrap.env` containing one line, with the real value filled in there:

   ```text
   ConnectionStrings__WmsConnection=Server=<SQL_HOST>,1433;Database=<DB_NAME>;User Id=<DB_USER>;Password=<DB_PASSWORD>;Encrypt=True
   ```

   Keep the file outside the repository, restrict it to the deployment account (`chmod 600 /etc/wms/bootstrap.env`), and set its SQL encryption options to match the server certificate. Do not put the connection string or admin password in a shell command.

3. Run this command from an interactive VPS terminal. Replace `<WMS_PRIVATE_NETWORK>` with the Docker network shared with SQL Server:

   ```sh
   docker run --rm -it --network <WMS_PRIVATE_NETWORK> \
     --env-file /etc/wms/bootstrap.env \
     --mount type=bind,source=/opt/wms/bootstrap-admin,target=/app,readonly \
     mcr.microsoft.com/dotnet/aspnet:8.0 \
     dotnet /app/WMS.BootstrapAdmin.dll
   ```

   Enter the email, full name, password, and password confirmation at the prompts. The password is not echoed or accepted as a command-line argument. Use 16 or more characters and no more than 72 UTF-8 bytes. Exit code `0` means the account and role assignment committed; `1` means it was refused or the database operation failed; `2` means the invocation or input was invalid.

The utility normalizes the email, checks for duplicates, looks up `SystemAdmin` by name, and refuses to create another account with that role, including when the existing account is inactive. It saves the user and role assignment in a serializable SQL transaction. If an error occurs during commit, inspect the database before retrying. Remove the published maintenance files and private bootstrap environment file from the VPS after successful use.

---

**Ahmad Edais**
