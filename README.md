# Enterprise Multi-Tenant Warehouse & Fulfillment System (WMS)

> A highly concurrent, multi-tenant Warehouse Management System (WMS) built to enterprise standards using C#/.NET. Demonstrates mastery of Clean Architecture, CQRS with MediatR, Domain-Driven Design, strict data isolation boundaries, and EF Core advanced features like Global Query Filters without relying on EF Migrations.

This is a backend portfolio project built strictly for learning, demonstrating, and applying advanced enterprise software engineering patterns. It is a headless REST API designed to solve complex domain problems such as race conditions, transactional integrity, and data isolation.

---

## 🏗️ Core Architectural Patterns

This project intentionally avoids the "generic CRUD" approach, opting instead for patterns found in high-scale, legacy-integrated enterprise environments:

* **Clean Architecture:** Strict 4-layer dependency inversion (Domain ⭠ Application ⭠ Infrastructure ⭠ WebApi). The Domain layer has zero external dependencies.
* **CQRS with Vertical Slicing:** Implemented via MediatR. Every business use case is a distinctly separated Command or Query. Controllers are kept "skinny," only dispatching requests.

* **"Manual Code-First" Database Strategy:** *EF Core Migrations are explicitly not used.* The database schema is the ultimate source of truth, maintained via a SQL Server Database Project (`.sqlproj`). Pure C# POCOs are mapped to this schema manually using the EF Core Fluent API (`IEntityTypeConfiguration`).
* **Multi-Tenancy:** Shared-database, row-level data isolation enforced transparently via EF Core Global Query Filters. A custom `ITenantContext` intercepts requests to guarantee data boundaries.

## ⚙️ Key Business Workflows

The system models the physical reality of a distribution center across three main workflows:

1.  **Inbound (Receiving):** Full transactional flow for receiving goods against an approved Purchase Order. Updates stock levels, records immutable ledger entries, and updates PO statuses atomically.
2.  **Outbound (Fulfillment & Allocation):** Solves heavy race conditions when multiple operators attempt to claim the last unit of stock. This uses database-level optimistic concurrency and conditional check-and-decrement logic to safely prevent overselling without heavy application-level pessimistic locks.
3.  **Real-Time Stock Monitoring:** A dual-path alerting system. When stock drops below a threshold, a Domain Event triggers a real-time SignalR WebSocket push to the tenant's dashboard. A background Worker Service (`BackgroundService`) acts as a periodic polling safety net.

## 🛠️ Tech Stack

* **Runtime & API:** .NET 8 (LTS), ASP.NET Core 8 Web API
* **Database & ORM:** SQL Server 2022, Entity Framework Core 8
* **Schema Management:** `Microsoft.Build.Sql` (SDK-style `.sqlproj`)
* **Architecture Tooling:** MediatR 12.x, FluentValidation 11.x
* **Real-time & Background Processing:** ASP.NET Core SignalR, `Microsoft.Extensions.Hosting.BackgroundService`
* **Security & Identity:** JWT Bearer Authentication, Custom ASP.NET PasswordHasher (PBKDF2)
* **API Documentation:** Swashbuckle (Swagger / OpenAPI)

## 📁 Project Structure

The repository is organized by physical dependency layers:

```text
├── WMS.Domain/               # Pure POCOs, Domain Events, Custom Exceptions
├── WMS.Application/          # MediatR Commands/Queries, Validators, DTOs (Organized by Vertical Slices)
├── WMS.Infrastructure/       # EF Core DbContext, Fluent API Configs, JWT Issuer, Interceptors, SignalR
├── WMS.WebApi/               # Controllers, Middleware, Program.cs Composition Root
└── WMS.DatabaseProject/      # .sqlproj containing raw T-SQL table definitions and constraints
```
---
> **Ahmad Edais**
> *Software Developer | Amman, Jordan*
>
> [LinkedIn](https://linkedin.com/in/ahmad-edais) • [Email](mailto:ahmad.edais.jo@gmail.com)
