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

---

**Ahmad Edais**
