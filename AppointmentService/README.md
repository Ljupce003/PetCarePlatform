# Appointment Service

Notes on the integration points that are deliberately left as scaffolding for now, so whoever
wires up the real infrastructure (or future you) doesn't have to reverse-engineer the plan.

## Testing

Four test projects, run with `dotnet test PetCarePlatform.slnx`:

| Project | Covers |
|---|---|
| `AppointmentService.Domain.Tests` | `Appointment`/`AvailabilitySlot` status-transition and booking rules, `Clinic`/`Veterinarian` construction guards. Pure unit tests, no dependencies beyond Domain. |
| `AppointmentService.Application.Tests` | Every command/query handler, with `IAppointmentRepository`/`IAvailabilitySlotRepository`/etc., `IPetVerificationClient`, and `IIntegrationEventPublisher` mocked (Moq). Covers the happy path, validation failures, domain exceptions (already-booked/expired slot, invalid status transition), and that a **failed Kafka publish doesn't fail an otherwise-successful booking/cancel/reschedule** — see `ScheduleAppointmentHandlerTests.HandleAsync_WhenEventPublishFails_StillReturnsTheBookedAppointment`. |
| `AppointmentService.Api.IntegrationTests` | Boots the real API (`WebApplicationFactory<Program>`) — real controllers, real `[Authorize]`/role checks, real domain rules — against an EF Core **InMemory** database and an in-memory `FakeIntegrationEventPublisher` instead of Postgres/Kafka. JWT login/validation goes through a real Keycloak (**must be running** — `docker compose up keycloak`), so this project isn't fully Docker-free. Covers `/health`, `/auth/login`, 401/403 authorization checks, and a full schedule → reschedule → cancel lifecycle asserting both the HTTP responses **and** that each step published the right event (`AppointmentScheduledEvent`/`AppointmentRescheduledEvent`/`AppointmentCancelledEvent`) to `petcare.appointments`. |
| `tests/AppointmentService.PactTests` | Consumer-side Pact tests (PactNet v4) for the `GET /api/pets/{petId}/exists?ownerId={ownerId}` contract `PetServiceClient` depends on — exists/owned, exists/not-owned, and not-found. Regenerates `/pacts/Appointment Service-Pet Service.json` on every run, which is what Pet Service's own (not-yet-written) provider-verification tests would check against. |

Notes on choices that might look surprising:

- **InMemory instead of Testcontainers/a real Postgres for integration tests.** Faster, and this
  project doesn't lean on Postgres-specific behavior (no raw SQL, no database-level constraints
  the tests need to exercise) — the unique index on `(VeterinarianId, StartsAtUtc)` is redundant
  with `AvailabilitySlot.Reserve()`'s own double-booking guard, which the Application tests
  already cover directly. If that stops being true later, swap `UseInMemoryDatabase` for a
  Testcontainers-backed Postgres in `AppointmentServiceApiFactory`. Note this only removes the
  Postgres dependency — **Keycloak still has to be running** for JWT auth (see below), so these
  tests are not fully Docker-free.
- **`AppointmentDbInitializer.InitializeAsync`'s `Database.MigrateAsync()` doesn't run in tests**
  (the InMemory provider doesn't support it, and — separately — ASP.NET Core's test host factory
  intercepts `Program.cs` right after `Build()`, so the inline seeding block between `Build()` and
  `RunAsync()` never executes under `WebApplicationFactory` regardless). `AppointmentServiceApiFactory`
  registers its own `TestDataSeeder : IHostedService` that calls `Database.EnsureCreatedAsync()` +
  the newly-extracted `AppointmentDbInitializer.SeedIfEmptyAsync(...)` instead.
- **Integration tests log in for real** (`POST /auth/login` against the running test instance,
  which itself proxies to a real Keycloak — see "Security and authorization" below) rather than
  faking authentication: no test-only auth handler needed, at the cost of needing Keycloak up
  before running this test project.
- **`AppointmentWorkflowTests` gives every test its own fresh factory/database** (`IAsyncLifetime`,
  not `IClassFixture`) since those tests book/reschedule/cancel real slots and would otherwise
  collide with each other; the read-only test classes (`HealthEndpointTests`, `AuthTests`,
  `AuthorizationTests`) share one factory via `IClassFixture` since they never mutate state.
- **`System.Net.Http.Json`'s `ReadFromJsonAsync`/`GetFromJsonAsync` default to case-*sensitive*
  property matching**, but the API serializes camelCase (ASP.NET Core MVC's default) into PascalCase
  C# DTOs — every read in the integration tests passes `JsonDefaults.CaseInsensitive` explicitly to
  avoid silently deserializing everything to default values instead of failing loudly.

Once test projects exist, the GitHub Actions workflow's commented-out `dotnet test` step can be
uncommented.

## Security and authorization

Keycloak is real (`infrastructure/keycloak/petcare-realm.json`, seeded by Member 1's
shared-infrastructure work), and this service only ever trusts it — no locally-signed fallback for
incoming tokens, in any environment. `Program.cs`'s `AddJwtBearer` always sets
`options.Authority = Jwt:Authority` and validates against Keycloak's real JWKS/issuer, and
`POST /auth/login` (`AuthController`) always proxies the given username/password straight to
Keycloak's token endpoint via `KeycloakAuthClient` (Resource Owner Password Credentials grant,
public `petcare-demo` client) and returns Keycloak's own signed token. There is no dev-only
bypass — **Keycloak must be reachable (`docker compose up keycloak`) for this service to start up
successfully or for any of the `AppointmentService.Api.IntegrationTests` to pass.**

### Test users

One per role, seeded in Keycloak's realm import (`infrastructure/keycloak/petcare-realm.json`),
using ids that intentionally match this service's own seeded demo data:

| Username | Password    | Role          | User id (JWT `sub`)                     |
|----------|-------------|---------------|------------------------------------------|
| `owner1` | `Owner123!` | `owner`       | `33333333-3333-3333-3333-333333333333` (= `DemoOwnerId`) |
| `vet1`   | `Vet123!`   | `veterinarian`| `22222222-2222-2222-2222-222222222221` (= `DemoVeterinarianId`) |
| `admin1` | `Admin123!` | `admin`       | `55555555-5555-5555-5555-555555555553` |

`owner1`/`vet1` deliberately reuse `AppointmentDbInitializer`'s demo ids, so logging in as
`owner1` (locally or through real Keycloak) gives you a token for the same owner that already has
a seeded appointment.

### Logging in (`POST /auth/login`) and calling the API from Swagger

1. `POST /auth/login` with one of the usernames/passwords above → returns
   `{ "accessToken": "...", "role": "...", "userId": "..." }`.
2. In Swagger UI (`/swagger`), click **Authorize** (top right) and paste just the `accessToken`
   value — no `Bearer ` prefix needed, Swagger adds it. Every protected endpoint now sends that
   token automatically.
3. The `.http` file captures the token into a variable automatically (see the `client.global.set`
   scripts after each `/auth/login` call) so the requests below it can reuse `{{ownerToken}}` /
   `{{adminToken}}` / `{{vetToken}}` directly.

### Authorization rules

- Every controller requires `[Authorize]` (any logged-in role) except `AuthController`
  (`/auth/login` — `[AllowAnonymous]`, obviously) and the health check endpoints (no `[Authorize]`
  metadata at all, since Consul's own health check hits `/health` unauthenticated).
- `POST /appointments`, `DELETE /appointments/{id}`, `PUT /appointments/{id}/reschedule` additionally
  require `[Authorize(Roles = "owner,admin")]` — a `veterinarian` token can browse everything but
  can't book/cancel/reschedule (try it in the `.http` file, look for the 403).
- **Known gap:** queries that take an `ownerId`/similar parameter (e.g.
  `GET /appointments/upcoming?ownerId=...`) don't yet check that the caller's own `sub` claim
  matches the id in the query — any authenticated user can currently query any owner's
  appointments. The demo Keycloak, Pet, and Appointment seeds now use aligned owner IDs, so adding
  that subject/owner authorization check is a separate hardening task rather than an integration
  blocker.

### Service-to-service authentication

`KeycloakServiceAccessTokenProvider` obtains a real OAuth2 client-credentials token for the
confidential `appointment-service` client, caches it until shortly before expiry, and
`ServiceAccessTokenHandler` attaches it to every outgoing Pet Service call. Pet Service validates
the issuer, audience, and `service` role before serving its ownership endpoint.

## Kafka integration events

Implemented in `Shared/Messaging/KafkaMessaging.cs` (shared building block, same reasoning as the
Consul code — reusable by every service, not Appointment-Service-specific) and the event contracts
in `Shared/AppointmentEvents/` (already defined before this section — the field shapes below are
whatever Treatment & Notification Service expects to consume).

Published on topic `petcare.appointments` (`PetCareTopics.Appointments`), one event per lifecycle
transition:

- **`AppointmentScheduledEvent`** — after `ScheduleAppointmentHandler` commits a new booking.
- **`AppointmentCancelledEvent`** — after `CancelAppointmentHandler` commits a cancellation.
- **`AppointmentRescheduledEvent`** — after `RescheduleAppointmentHandler` commits a slot change.

Every event goes out wrapped in an `IntegrationEventEnvelope` (`EventType`, `Payload` as JSON,
`OccurredAtUtc`, `CorrelationId`) — consumers can route on `EventType` without deserializing the
payload first. `CorrelationId` (and the Kafka message key) is the event's own `EventId`, so
retries/replays of the same occurrence land on the same partition in order.

Reliability / idempotency: the producer is configured with `Acks.All` + `EnableIdempotence = true`
(the producer-side guarantee against duplicate/reordered records on retry). Publishing only ever
happens **after** the database commit succeeds, and a failed publish is logged as a warning, not
thrown — Kafka being briefly unreachable doesn't turn an already-successful booking/cancel/
reschedule into an error response to the caller. This is "at-least-once, non-blocking," not a full
transactional outbox — if you need a stronger guarantee later (no dropped events if Kafka is down
for longer than a request), the next step is writing the event to an outbox table in the same
transaction as the appointment change and having a background process publish from there instead.

Configuration (`appsettings.json`, overridable via `Kafka__*` env vars):

```json
"Kafka": { "BootstrapServers": "localhost:29092", "ClientId": "appointment-service" }
```

In Docker (`docker-compose.yml`), a single-node KRaft-mode `kafka` container (`apache/kafka:4.1.0`,
no Zookeeper needed) is started; `appointment-service` points at `kafka:9092` (the internal
listener) once it's healthy.

**To test:** call `POST /appointments` (or `DELETE /appointments/{id}`, `PUT
/appointments/{id}/reschedule`) from Swagger or the `.http` file — each one publishes to
`petcare.appointments` after it responds. To see the raw
messages without waiting for Treatment & Notification Service to consume them, exec into the
Kafka container:

```sh
docker exec -it petcare-kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 --topic petcare.appointments --from-beginning
```

## Keycloak / client-credentials authentication

The Appointment Service calls the Pet Service over HTTP (`IPetVerificationClient` →
`PetServiceClient`, in `AppointmentService.Infrastructure/Clients/`). Service-to-service calls
like this one carry an OAuth2 **client-credentials** access token issued by Keycloak. The realm
import (`infrastructure/keycloak/petcare-realm.json`) defines `appointment-service` as a
confidential service-account client with the `service` role. What's in place:

- `AppointmentService.Infrastructure/Security/ServiceAccessTokenHandler.cs` — a `DelegatingHandler`
  wired into the Pet Service `HttpClient` pipeline. It attaches
  `Authorization: Bearer <token>` to every outgoing request automatically.
- `IServiceAccessTokenProvider` — the abstraction `ServiceAccessTokenHandler` asks for a token.
- `KeycloakServiceAccessTokenProvider` — POSTs `grant_type=client_credentials` to Keycloak and
  caches the returned token until the configured refresh-skew window.
- `Jwt:ClientId`, `Jwt:ClientSecret`, and `Jwt:ServiceTokenRefreshSkewSeconds` — runtime settings;
  Docker injects the demo secret used by the realm import.
- Pet Service's `service` role policy — validates and authorizes the resulting bearer token on
  `GET /api/pets/{petId}/exists`.

## Consul / service discovery

Implemented in `Shared/ServiceDiscovery/ConsulIntegration.cs` (shared building block, not
Appointment-Service-specific, so PetService/TreatmentAndNotificationService can reuse it the same
way). Wired up in `AppointmentService.Infrastructure/DependencyInjection.cs` via
`services.AddPetCareConsul(configuration)`.

What it does:

- **Registers this instance in Consul on startup**, and deregisters it on shutdown
  (`ConsulRegistrationHostedService`). The registration includes an HTTP health check pointed at
  this service's own `/health` endpoint (`Interval: 10s`), so Consul automatically stops routing
  to an instance that turns unhealthy — no extra health check code needed, it reuses the one
  from section 1.
- **Exposes `IConsulServiceResolver`** (`ResolveAsync`/`ResolveAllAsync`) so any client in this
  service can ask Consul "who is currently healthy for service X" instead of hardcoding an
  address.
- **`ServiceDiscoveryHandler`** — a `DelegatingHandler` that, if added to an `HttpClient`'s
  pipeline, rewrites requests aimed at a logical `http://<name>-service/...` host to whatever
  Consul currently reports as the healthy address/port for `<name>-service`.
- Registration failures (e.g. running `dotnet run` locally without Consul up) are logged and
  retried by the background registration loop. The service starts and joins Consul when it becomes
  available.

Configuration (`appsettings.json`, overridable via `Consul__*`/`ServiceRegistration__*` env vars):

```json
"Consul": { "Address": "http://localhost:8500" },
"ServiceRegistration": {
  "Name": "appointment-service",
  "Id": "appointment-service-1",
  "Address": "localhost",
  "Port": 5138
}
```

In Docker (`docker-compose.yml`), a `consul` container (`hashicorp/consul:1.21.5`, dev mode, UI on
`http://localhost:8500`) is started, and `appointment-service`'s registration address/port point at
its container name/port so other containers can reach it.

`PetServiceClient` uses the logical base address `http://pet-service`. Its HTTP pipeline obtains a
service token, asks Consul for a healthy `pet-service` instance, rewrites the request to that
address, and applies the standard resilience handler. Both Pet and Appointment refresh their
Consul registrations in the background, so discovery remains active after Consul restarts or
temporarily loses its catalog state.

## MCP contribution

There is now one real shared MCP server for the whole team, the top-level `MCPServer` project —
not something each service hosts itself. Member 2's appointment-related tools
(`Member-2-Tasks.md`, section 10) live there, in `MCPServer/Tools/AppointmentTools.cs`, calling this
service over plain REST through `MCPServer/Clients/AppointmentServiceClient.cs`. An earlier version
of this exposed the same tools directly from this service's own process at `POST /mcp`; that's been
removed in favor of the real shared server now that it exists (see that project's own docs for the
current tool list and how `BearerTokenForwardingHandler` forwards the caller's own Keycloak token
downstream, so — unlike the old in-process version — write actions like opening a slot are safe:
they run as the actual authenticated caller, not as this service impersonating "some admin").

The one REST addition this made necessary: `GET /veterinarians/available?date=&location=&specialization=`
(`VeterinariansController.Available`), backed by `FindAvailableVeterinariansHandler`
(`AppointmentService.Application/Queries/FindAvailableVeterinarians.cs`) — a normal Application-layer
handler like any other, covered by its own tests in `AppointmentService.Application.Tests`, just
also reachable over REST now so the external MCP server can call it in one round trip instead of
re-implementing the clinic/slot join itself. It composes two existing reads (`IClinicRepository` +
`IAvailabilitySlotRepository`) rather than adding a new repository query, since clinics only carry
their own `Location` and slot search results don't.

`POST /slots` (admin-only, `AvailabilitySlotsController.Create`) is the other endpoint the MCP
server's write tool uses to open new slots.

## Pet Service contract this service depends on

`PetServiceClient` calls:

```
GET /api/pets/{petId}/exists?ownerId={ownerId}
```

Expected responses:
- `200 OK` with body `{ "exists": true, "ownedByOwner": true }` (or `false`/`false`,
  `true`/`false`, etc.)
- `404 Not Found` is also treated as "pet does not exist" (`Exists = false`).

Pet Service implements this endpoint (including the `/api` compatibility route), validates the
Appointment Service's Keycloak token, and returns ownership from its real PostgreSQL data. The
private `PetExistsResponse` mapping remains the anti-corruption layer between the two services.

### Isolated development without Pet Service

`AppointmentService.Infrastructure/Clients/FakePetVerificationClient.cs` remains available only so
Appointment Service can be run and tested in isolation when Pet Service is intentionally absent:

- Controlled by `PetService:UseFakeVerification` — `true` only in
  `appsettings.Development.json` for a standalone `dotnet run`. The default is `false`, and Docker
  explicitly uses `false`, so the composed platform always exercises the real integration.
- Accepts any non-empty `petId`/`ownerId` as a valid, owned pet — it doesn't try to fake Pet
  Service's actual data, just unblocks testing this service's own logic (slot reservation,
  double-booking, Kafka events, etc.) independently of Pet Service's progress.
- The demo `DemoPetId` (`44444444-4444-4444-4444-444444444444`) and `DemoOwnerId`
  (`33333333-3333-3333-3333-333333333333`) from `AppointmentDbInitializer` are convenient to reuse
  because the same IDs are seeded by Pet Service and `DemoOwnerId` already has one seeded
  appointment. In fake mode any non-empty IDs are accepted; in the composed platform the IDs must
  exist and have the requested ownership relation in Pet Service.
