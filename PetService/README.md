# Pet Service

Owner of the **Pet bounded context**. This service owns the `petcare_pet` schema exclusively —
no other service reads or writes it.

> **Status:** task 1 (*service setup and architecture*) only. The layers, configuration, and
> infrastructure are in place; the domain model, use cases, and REST endpoints come in tasks 2–5.

## Project structure

Clean Architecture, four projects. Dependencies point inwards only — the Domain project has no
references at all, so domain logic can never take a dependency on EF Core or ASP.NET Core.

```
PetService.Api             → Application, Infrastructure   host, Swagger, health checks, logging
PetService.Infrastructure  → Application, Domain           EF Core / PostgreSQL, DI wiring
PetService.Application     → Domain                        use cases, DTOs, validators
PetService.Domain          → (nothing)                     entities, value objects, domain rules
```

| Project | Contains today | Arrives in |
| --- | --- | --- |
| `PetService.Domain` | — | Owner and Pet aggregates, value objects (task 2) |
| `PetService.Application` | `AddPetServiceApplication()` | use cases, DTOs, validators (task 3) |
| `PetService.Infrastructure` | `AddPetServiceInfrastructure()`, `PetDbContext` | entity mappings, repositories, seed data (task 4) |
| `PetService.Api` | host, Swagger, health checks | controllers, authentication (tasks 5–6) |

## Dependency injection

Each layer registers itself and the API composes exactly one call:

```csharp
builder.Services.AddPetServiceInfrastructure(builder.Configuration);
```

`AddPetServiceInfrastructure` registers `PetDbContext` and then calls `AddPetServiceApplication`,
so the API never needs to know what lives inside either layer. New services get registered inside
their own layer's extension method rather than in `Program.cs`.

## Configuration

| Setting | Local default | Docker |
| --- | --- | --- |
| `ConnectionStrings:Database` | `Host=localhost;Port=5433;…` | `ConnectionStrings__Database` env var → `Host=postgres;Port=5432;…` |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Docker` |

Startup fails with an explicit message if the connection string is missing, rather than surfacing
a confusing Npgsql error later.

## Logging

`Development` uses the single-line console formatter for readability. Every other environment uses
the JSON console formatter with scopes enabled, so a log collector can index the fields:

```json
{"EventId":14,"LogLevel":"Information","Category":"Microsoft.Hosting.Lifetime",
 "Message":"Now listening on: http://[::]:8080","State":{"address":"http://[::]:8080"},"Scopes":[]}
```

## Endpoints

| Route | Purpose |
| --- | --- |
| `/swagger` | Swagger UI |
| `/openapi/v1.json` | OpenAPI document |
| `/health` | full report, including a PostgreSQL connectivity check |
| `/health/live` | liveness only, does not touch the database |

`/health` returns which check failed rather than a one-word body:

```json
{"status":"Healthy","totalDurationMs":15.6,
 "checks":[{"name":"pet-database","status":"Healthy","description":null}]}
```

## Running

### Locally, with PostgreSQL in Docker

```bash
docker compose up -d postgres
```

Then start the API — F5 in Visual Studio, or:

```bash
dotnet run --project PetService/PetService.Api
```

Swagger opens at <http://localhost:5224/swagger>.

### Everything in Docker

```bash
docker compose up -d --build
```

Swagger is then at <http://localhost:5101/swagger>. PostgreSQL is published on host port **5433**
so a locally installed instance on 5432 stays out of the way; the service waits for the database's
health check before starting.

## Verified

- `dotnet build` succeeds for the whole solution.
- Running locally against the compose PostgreSQL: `/swagger` renders, `/health` reports `Healthy`
  with the `pet-database` check passing, `/health/live` returns `Healthy`.
- `docker compose up --build`: the container starts, reaches PostgreSQL, serves `/swagger` and
  `/health` on port 5101, and emits clean JSON logs with no warnings or errors.
