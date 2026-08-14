# Appointment Service

Notes on the integration points that are deliberately left as scaffolding for now, so whoever
wires up the real infrastructure (or future you) doesn't have to reverse-engineer the plan.

## Testing

Four test projects, run with `dotnet test PetCarePlatform.slnx`:

| Project | Covers |
|---|---|
| `AppointmentService.Domain.Tests` | `Appointment`/`AvailabilitySlot` status-transition and booking rules, `Clinic`/`Veterinarian` construction guards. Pure unit tests, no dependencies beyond Domain. |
| `AppointmentService.Application.Tests` | Every command/query handler, with `IAppointmentRepository`/`IAvailabilitySlotRepository`/etc., `IPetVerificationClient`, and `IIntegrationEventPublisher` mocked (Moq). Covers the happy path, validation failures, domain exceptions (already-booked/expired slot, invalid status transition), and that a **failed Kafka publish doesn't fail an otherwise-successful booking/cancel/reschedule** — see `ScheduleAppointmentHandlerTests.HandleAsync_WhenEventPublishFails_StillReturnsTheBookedAppointment`. |
| `AppointmentService.Api.IntegrationTests` | Boots the real API (`WebApplicationFactory<Program>`) — real controllers, real `[Authorize]`/role checks, real JWT login/validation, real domain rules — against an EF Core **InMemory** database and an in-memory `FakeIntegrationEventPublisher` instead of Postgres/Kafka, so the whole suite runs without Docker. Covers `/health`, `/auth/login` + `/auth/token`, 401/403 authorization checks, and a full schedule → reschedule → cancel lifecycle asserting both the HTTP responses **and** that each step published the right event (`AppointmentScheduledEvent`/`AppointmentRescheduledEvent`/`AppointmentCancelledEvent`) to `petcare.appointments`. |
| `tests/AppointmentService.PactTests` | Consumer-side Pact tests (PactNet v4) for the `GET /api/pets/{petId}/exists?ownerId={ownerId}` contract `PetServiceClient` depends on — exists/owned, exists/not-owned, and not-found. Regenerates `/pacts/Appointment Service-Pet Service.json` on every run, which is what Pet Service's own (not-yet-written) provider-verification tests would check against. |

Notes on choices that might look surprising:

- **InMemory instead of Testcontainers/a real Postgres for integration tests.** Faster, no Docker
  requirement for CI, and this project doesn't lean on Postgres-specific behavior (no raw SQL,
  no database-level constraints the tests need to exercise) — the unique index on
  `(VeterinarianId, StartsAtUtc)` is redundant with `AvailabilitySlot.Reserve()`'s own
  double-booking guard, which the Application tests already cover directly. If that stops being
  true later, swap `UseInMemoryDatabase` for a Testcontainers-backed Postgres in
  `AppointmentServiceApiFactory`.
- **`AppointmentDbInitializer.InitializeAsync`'s `Database.MigrateAsync()` doesn't run in tests**
  (the InMemory provider doesn't support it, and — separately — ASP.NET Core's test host factory
  intercepts `Program.cs` right after `Build()`, so the inline seeding block between `Build()` and
  `RunAsync()` never executes under `WebApplicationFactory` regardless). `AppointmentServiceApiFactory`
  registers its own `TestDataSeeder : IHostedService` that calls `Database.EnsureCreatedAsync()` +
  the newly-extracted `AppointmentDbInitializer.SeedIfEmptyAsync(...)` instead.
- **Integration tests log in for real** (`POST /auth/login` against the running test instance)
  rather than faking authentication — a direct payoff of section 9's JWTs being locally self-
  issued and self-validated: no test-only auth handler needed.
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

Keycloak doesn't exist in this repo yet (Member 1's shared-infrastructure task), so this service
issues and validates its **own** JWTs in the meantime — same shape/claims a real identity provider
would produce, just backed by a fixed in-memory user/client list instead of a real store. Nothing
downstream (`[Authorize]`, role checks, `ServiceAccessTokenHandler`) needs to change when Keycloak
shows up — only the two places listed under "Swapping in Keycloak" below.

### Test users (`AppointmentService.Infrastructure/Security/TestUsers.cs`)

One per role, as requested:

| Username | Password    | Role          | User id (JWT `sub`)                     |
|----------|-------------|---------------|------------------------------------------|
| `owner1` | `Owner123!` | `owner`       | `33333333-3333-3333-3333-333333333333` (= `DemoOwnerId`) |
| `vet1`   | `Vet123!`   | `veterinarian`| `22222222-2222-2222-2222-222222222221` (= `DemoVeterinarianId`) |
| `admin1` | `Admin123!` | `admin`       | `55555555-5555-5555-5555-555555555553` |

`owner1`/`vet1` deliberately reuse `AppointmentDbInitializer`'s demo ids, so logging in as
`owner1` gives you a token for the same owner that already has a seeded appointment.

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
  (`/auth/login`, `/auth/token` — `[AllowAnonymous]`, obviously) and the health check endpoints
  (no `[Authorize]` metadata at all, since Consul's own health check hits `/health` unauthenticated).
- `POST /appointments`, `DELETE /appointments/{id}`, `PUT /appointments/{id}/reschedule` additionally
  require `[Authorize(Roles = "owner,admin")]` — a `veterinarian` token can browse everything but
  can't book/cancel/reschedule (try it in the `.http` file, look for the 403).
- **Known gap:** queries that take an `ownerId`/similar parameter (e.g.
  `GET /appointments/upcoming?ownerId=...`) don't yet check that the caller's own `sub` claim
  matches the id in the query — any authenticated user can currently query any owner's
  appointments. Fixing this properly needs a stable mapping between Pet Service's/Keycloak's owner
  identity and the `ownerId` GUIDs used throughout this service, which doesn't exist yet either.

### Service-to-service authentication

`LocalServiceAccessTokenProvider` (`AppointmentService.Infrastructure/Security/`) replaces the old
no-op `NullServiceAccessTokenProvider`: every outgoing call to Pet Service now carries a real,
signed bearer token (role `service`, `client_id: appointment-service`), attached the same way as
before via `ServiceAccessTokenHandler`. Pet Service doesn't validate it yet (it has no JWT bearer
authentication wired up), so this doesn't do anything end-to-end yet — but this service's half of
"authenticate correctly" is now real rather than sending no token at all.

### Swapping in Keycloak later

1. Point `AddJwtBearer`'s `TokenValidationParameters` (`Program.cs`) at Keycloak's issuer/JWKS
   instead of the local symmetric key (`options.Authority = "http://keycloak:8080/realms/petcare"`
   is normally enough — Keycloak's own metadata endpoint supplies the rest).
2. Replace `LocalServiceAccessTokenProvider`'s registration in
   `AppointmentService.Infrastructure/DependencyInjection.cs` with a real implementation that POSTs
   `grant_type=client_credentials` to Keycloak's token endpoint and caches the result (see the
   still-relevant steps under "Keycloak / client-credentials authentication" below, which predate
   this section).
3. `AuthController`, `JwtTokenService`, `TestUsers`, `TestClients` can all be deleted — real login
   happens against Keycloak directly (or via the API Gateway), not this service.
4. `[Authorize]`/`[Authorize(Roles = ...)]` on the controllers don't change at all — they just
   start validating real tokens instead of local ones.

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
like this one are supposed to carry an OAuth2 **client-credentials** access token, but there is
no Keycloak (or any identity provider) running anywhere in this repo yet — that's Member 1's
"Keycloak" shared-infrastructure task.

**Update (section 9):** this no longer sends no token at all — see "Security and authorization"
above. `LocalServiceAccessTokenProvider` now attaches a real, locally-signed token; Pet Service
just doesn't validate it yet. What's in place:

- `AppointmentService.Infrastructure/Security/ServiceAccessTokenHandler.cs` — a `DelegatingHandler`
  already wired into the Pet Service `HttpClient` pipeline. It attaches
  `Authorization: Bearer <token>` to every outgoing request automatically.
- `IServiceAccessTokenProvider` — the abstraction `ServiceAccessTokenHandler` asks for a token.
- `LocalServiceAccessTokenProvider` — the implementation registered today. Issues a token via
  `JwtTokenService` for `TestClients.AppointmentService` (`appointment-service` /
  `appointment-secret`). Registered in `AddPetServiceClient(...)` inside
  `AppointmentService.Infrastructure/DependencyInjection.cs`.
- `NullServiceAccessTokenProvider` — still present, unregistered, kept only as the "send nothing"
  reference implementation.

### What to do once Keycloak exists

1. Create a confidential client in the `petcare` realm for this service (e.g. client id
   `appointment-service`), with the client-credentials grant enabled. Note its client secret.
2. Add config to `appsettings.json` (and the `PetService__BaseUrl`-style env var overrides in
   `docker-compose.yml`):
   ```json
   "Keycloak": {
     "TokenEndpoint": "http://keycloak:8080/realms/petcare/protocol/openid-connect/token",
     "ClientId": "appointment-service",
     "ClientSecret": "<from step 1>"
   }
   ```
3. Implement a real `IServiceAccessTokenProvider` (e.g. `KeycloakServiceAccessTokenProvider`)
   that POSTs `grant_type=client_credentials` to the token endpoint and **caches** the token
   until shortly before it expires (don't request a new one on every call).
4. Swap the registration in `AddPetServiceClient(...)`:
   ```csharp
   services.AddSingleton<IServiceAccessTokenProvider, KeycloakServiceAccessTokenProvider>();
   ```
   (replacing `LocalServiceAccessTokenProvider`). Nothing else changes — `ServiceAccessTokenHandler`
   and `PetServiceClient` already only depend on the interface.
5. The Pet Service (and any other service we call) needs to actually validate that token on its
   side too (JWT bearer authentication, audience/issuer checks) — that's a separate piece of work
   on each service that owns an endpoint we call.
6. Also swap `AddJwtBearer`'s validation (`Program.cs`) and delete the local login/token
   scaffolding — see "Swapping in Keycloak later" under "Security and authorization" above for the
   full list.

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
- Registration failures (e.g. running `dotnet run` locally without Consul up) are logged as a
  warning, not thrown — same "degrade gracefully" pattern used for the database and the Pet
  Service client, so the service still starts.

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

**Not yet wired up:** `PetServiceClient`'s `HttpClient` still uses the static `PetService:BaseUrl`
directly (unchanged from section 5) — `ServiceDiscoveryHandler` is *not* in its pipeline. That's
deliberate: Pet Service doesn't register itself in Consul yet, so resolving `http://pet-service/`
through Consul would just fail with "no healthy instances found" and break a currently-working
integration. Once Pet Service adds its own `ConsulRegistrationHostedService` (or equivalent) and
you want Appointment Service to discover it dynamically instead of reading a fixed URL from
config, the change here is small:

1. Change `PetService:BaseUrl` to a logical host ending in `-service`, e.g. `http://pet-service/`.
2. Add `.AddHttpMessageHandler<ServiceDiscoveryHandler>()` to the
   `AddHttpClient<IPetVerificationClient, PetServiceClient>(...)` chain in
   `AppointmentService.Infrastructure/DependencyInjection.cs` (alongside the existing
   `ServiceAccessTokenHandler` and resilience handler).

Nothing in `PetServiceClient` itself needs to change — it only ever sees `HttpClient.BaseAddress`.

## MCP contribution

Member 2's appointment-related tools for the shared MCP server (`Member-2-Tasks.md`, section 10)
are exposed directly from this service's own process, at `POST /mcp` — not a separate project.
`AppointmentService.Api/Mcp/AppointmentTools.cs` is a thin `[McpServerToolType]` wrapper around the
existing Application-layer query handlers (the same ones the REST controllers call), resolved from
the same DI container as the rest of the API — no second copy of any business rule, no extra
network hop, no separate auth story.

Tools:

- `FindAvailableVeterinarians(date, location?, specialization?)` and
  `GetUpcomingAppointments(ownerId)` — the two required by the task list.
- `SearchClinics(location?)`, `SearchVeterinarians(clinicId?, specialization?)`,
  `SearchAvailableSlots(veterinarianId?, date?)` — the rest of the read-only query surface, for
  browsing individually instead of only through the composite search.
- `CreateAvailableSlot(veterinarianId, startsAtUtc, endsAtUtc)` — the one write tool. Opens a new
  slot for an existing veterinarian; same as `POST /slots` (admin-only over REST). Unlike
  booking/cancelling/rescheduling, opening a slot isn't done "on behalf of" a specific owner, so it
  doesn't have the same missing-identity problem — it's an administrative/scheduling action, closer
  to seeding demo data than to a customer action.

`FindAvailableVeterinarians` has no dedicated repository query behind it — clinics only carry their
own `Location`, and slot search results don't — so `FindAvailableVeterinariansHandler`
(`AppointmentService.Application/Queries/FindAvailableVeterinarians.cs`) composes two existing
reads: it resolves matching clinic ids from `IClinicRepository` (only when a location filter is
given) and filters/groups the open slots for the date from `IAvailabilitySlotRepository`
client-side. It's a first-class Application-layer handler like any other, registered in
`AddAppointmentServiceApplication` and covered by its own tests in
`AppointmentService.Application.Tests`.

Deliberately read-only: booking, cancelling and rescheduling stay REST-only endpoints
(`AppointmentsController`), since those actions need a specific, authenticated owner/admin — an MCP
tool call here has no such per-user identity to act as. `/mcp` itself is unauthenticated, the same
reasoning as `/health`: it re-uses the exact same validation as the REST endpoints and there's
nothing service-to-service to authenticate anymore now that everything runs in one process.

## Pet Service contract this service depends on

`PetServiceClient` calls:

```
GET /api/pets/{petId}/exists?ownerId={ownerId}
```

Expected responses:
- `200 OK` with body `{ "exists": true, "ownedByOwner": true }` (or `false`/`false`,
  `true`/`false`, etc.)
- `404 Not Found` is also treated as "pet does not exist" (`Exists = false`).

This endpoint doesn't exist on the Pet Service yet — it's listed under Pet Service's own task
list (section 7, "cross-service integration work"). If the actual shape ends up different, only
`PetServiceClient`'s private `PetExistsResponse` record and the two lines that map it to
`PetVerificationResult` need to change — that's the whole point of the anti-corruption layer.

### Testing without Pet Service: FakePetVerificationClient + demo IDs

Pet Service also has no seeded pets/owners yet, so even once `/exists` exists there's nothing real
to verify against locally. `AppointmentService.Infrastructure/Clients/FakePetVerificationClient.cs`
stands in for `PetServiceClient` so the whole booking workflow is still testable end-to-end:

- Controlled by `PetService:UseFakeVerification` — `true` in `appsettings.Development.json` (so
  `dotnet run` / Swagger just works) **and** currently also `true` via `PetService__UseFakeVerification`
  in `docker-compose.yml` for `appointment-service`, since Pet Service doesn't have a working
  `/exists` endpoint in Docker either yet. `appsettings.json`'s own default is `false`. Remove the
  `docker-compose.yml` override once Pet Service implements the real endpoint, so Docker goes back
  to exercising the real integration.
- Accepts any non-empty `petId`/`ownerId` as a valid, owned pet — it doesn't try to fake Pet
  Service's actual data, just unblocks testing this service's own logic (slot reservation,
  double-booking, Kafka events, etc.) independently of Pet Service's progress.
- The demo `DemoPetId` (`44444444-4444-4444-4444-444444444444`) and `DemoOwnerId`
  (`33333333-3333-3333-3333-333333333333`) from `AppointmentDbInitializer` are convenient to reuse
  since `DemoOwnerId` already has one seeded appointment, so `GET /appointments/upcoming` has
  something to show against the same owner right away — but any GUIDs work.
- Once Pet Service has a real `/exists` endpoint and real seed data, set
  `PetService:UseFakeVerification` to `false` (or delete the setting) to go back to the real
  `PetServiceClient` — no other code changes needed.
