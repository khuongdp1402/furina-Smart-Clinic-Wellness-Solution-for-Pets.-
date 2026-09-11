# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Furina — a multi-tenant SaaS backend for pet clinics ("Smart Clinic & Wellness Solution for Pets"):
service intake/triage, medication & supply inventory, boarding management, real-time financial
reporting. Work is tracked in Taiga (project `furinasmart-clinic-wellness-solution-for-pets`,
6 sprints from core tenant infra through Next.js admin portals); only Sprint 1's multi-tenant
database foundation (TASK-11) is built so far.

## Commands

```bash
# Build everything
dotnet build

# Run the API (needs ASPNETCORE_ENVIRONMENT=Development to pick up appsettings.Development.json,
# which holds the local dev connection string and Seed:RunOnStartup)
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/Furina.Api

# Local Postgres (also creates the non-superuser `furina_app` role via db/init/01-roles.sql)
docker compose up -d

# EF Core migrations — always run against the `postgres` superuser connection, never furina_app
# (furina_app can't create tables; RLS also blocks it from seeing across tenants during migration).
export FURINA_DB_CONNECTION="Host=localhost;Port=5433;Database=furina;Username=postgres;Password=furina_superuser_dev_pw"
dotnet ef migrations add <Name> --project src/Furina.Infrastructure --startup-project src/Furina.Infrastructure -o Persistence/Migrations
dotnet ef database update --project src/Furina.Infrastructure --startup-project src/Furina.Infrastructure
dotnet ef database update 0 --project src/Furina.Infrastructure --startup-project src/Furina.Infrastructure   # rollback all
```

There is no test project yet — TASK-11 was verified by hand against a real local Postgres container
(see README.md's "Test thủ công RLS" section for the exact psql commands). The first test project
should follow TDD conventions from the `superpowers` skill plugin (installed user-wide) once real
business logic beyond schema/seeding exists.

## Architecture

Three-project solution, dependency direction `Api → Infrastructure → Domain`:

- **Furina.Domain** — plain entities only (`Tenant`, `User`, `Role`, `UserRole`, `RefreshToken`).
  No EF Core references here. Tenant-scoped entities implement `ITenantScoped` (a `TenantId` prop).
- **Furina.Infrastructure** — `FurinaDbContext`, migrations, and the multi-tenancy plumbing.
- **Furina.Api** — ASP.NET Core host and DI composition root (`Program.cs`).

### Multi-tenancy: how a request ends up scoped to one tenant

This is the load-bearing mechanism of the whole system and the reason TASK-11 exists — get it wrong
and every later feature inherits a cross-tenant data leak.

1. Every tenant-scoped table has Postgres Row-Level Security **forced** on (`FORCE ROW LEVEL
   SECURITY`, not just `ENABLE`), with a policy checking
   `tenant_id = current_setting('app.tenant_id')::uuid`. This is in the migration's raw SQL
   (`migrationBuilder.Sql(...)` in `Persistence/Migrations/*_InitialCreate.cs`), not something EF
   models natively — any future tenant-scoped table's migration needs the same policy added by hand.
2. The app's Postgres connection (`furina_app`, created by `db/init/01-roles.sql`) is deliberately
   **not** a superuser and does not have `BYPASSRLS` — those bypass RLS entirely, at which point the
   policy in (1) becomes decorative. Migrations run as `postgres` instead, since RLS would otherwise
   block the migrator itself from seeing rows across tenants.
3. `ITenantContext` (scoped per HTTP request) holds the current tenant id. Nothing sets it yet in
   application code — the JWT + tenant-resolution middleware that will populate it per-request is
   the next Sprint 1 task (TASK-12), currently unbuilt.
4. `TenantConnectionInterceptor` (an EF `DbConnectionInterceptor`, scoped, wired in `Program.cs` via
   `AddInterceptors`) runs `SET app.tenant_id = ...` every time EF opens a new physical Postgres
   connection for the current scope, reading the value from `ITenantContext`. This re-fires on every
   connection open because EF/Npgsql pooling silently resets session-level `SET` state between
   commands — a one-off `SET` executed manually (e.g. in a seed script) does **not** survive to the
   next query. `SeedData.SeedAsync` demonstrates the correct pattern: call `tenantContext.SetTenant(...)`
   and let the interceptor keep reapplying it, rather than issuing `SET` directly.
5. `tenants` itself is the one table intentionally *without* RLS or a `TenantId` — it's the tenant
   root, not tenant-scoped data.

When adding a new tenant-scoped entity: give it `ITenantScoped`, map it in `FurinaDbContext`, and add
`ENABLE`/`FORCE ROW LEVEL SECURITY` + the `tenant_isolation` policy + a `GRANT` to `furina_app` in its
migration, following the existing `InitialCreate` migration as the template.
