# Appointment Service

Notes on the integration points that are deliberately left as scaffolding for now, so whoever
wires up the real infrastructure (or future you) doesn't have to reverse-engineer the plan.

## Keycloak / client-credentials authentication

The Appointment Service calls the Pet Service over HTTP (`IPetVerificationClient` →
`PetServiceClient`, in `AppointmentService.Infrastructure/Clients/`). Service-to-service calls
like this one are supposed to carry an OAuth2 **client-credentials** access token, but there is
no Keycloak (or any identity provider) running anywhere in this repo yet — that's Member 1's
"Keycloak" shared-infrastructure task.

What's already in place, waiting for it:

- `AppointmentService.Infrastructure/Security/ServiceAccessTokenHandler.cs` — a `DelegatingHandler`
  already wired into the Pet Service `HttpClient` pipeline. It attaches
  `Authorization: Bearer <token>` to every outgoing request automatically.
- `IServiceAccessTokenProvider` — the abstraction `ServiceAccessTokenHandler` asks for a token.
- `NullServiceAccessTokenProvider` — the only implementation that exists right now. It returns
  `null`, so requests currently go out **without** an Authorization header. This is registered in
  `AddPetServiceClient(...)` inside `AppointmentService.Infrastructure/DependencyInjection.cs`.

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
   Nothing else changes — `ServiceAccessTokenHandler` and `PetServiceClient` already only depend
   on the interface.
5. The Pet Service (and any other service we call) needs to actually validate that token on its
   side too (JWT bearer authentication, audience/issuer checks) — that's a separate piece of work
   on each service that owns an endpoint we call.

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
