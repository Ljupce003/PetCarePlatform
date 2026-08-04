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

Same situation: `PetService:BaseUrl` in `appsettings.json` is a plain config value
(`http://localhost:5224` locally, `http://pet-service:8080` in Docker via the
`PetService__BaseUrl` env var in `docker-compose.yml`). There's no Consul container and no
Consul client package anywhere in this repo yet.

Once Consul infrastructure exists (task list section 6), the swap is small: resolve the base
address through Consul's health API instead of reading it straight from config, before the
`AddHttpClient<IPetVerificationClient, PetServiceClient>(...)` call in
`AppointmentService.Infrastructure/DependencyInjection.cs`. `PetServiceClient` itself doesn't need
to change at all — it only ever sees `HttpClient.BaseAddress`.

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
