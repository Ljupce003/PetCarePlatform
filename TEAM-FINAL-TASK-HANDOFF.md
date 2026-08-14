# PetCare Platform — Final Team Task Handoff

This document is the current task list to use for the final project phase. It is grouped by service owner and then by shared team work.

Status:

- `[x]` complete
- `[ ]` still required
- **OUT OF SCOPE** deliberately not being implemented

## Member 1 — Pet Service owner

### Already complete

- [x] Pet Service with Domain, Application, Infrastructure, and API layers.
- [x] Owner and Pet domain models, validation, repositories, PostgreSQL persistence, migrations, and demo seed data.
- [x] Owner and Pet CRUD endpoints.
- [x] Pet ownership/verification endpoint: `GET /pets/{id}/exists?ownerId=...`.
- [x] JWT authentication and role-based authorization with Keycloak tokens.
- [x] Pet MCP tools: `get_pet` and `get_owner_pets`.
- [x] Swagger/OpenAPI and health endpoint.

### Remaining Member 1 work

- [ ] Register Pet Service in Consul with its `/health` check.
- [ ] Confirm the final pet-verification contract with Member 2. The implemented Pet route is `/pets/{id}/exists`; Appointment currently calls `/api/pets/{id}/exists`, so both sides and the Pact must use one identical path.
- [ ] Add Pet Service domain unit tests.
- [ ] Add Pet Service API/integration tests for main success, validation, authentication, authorization, not-found, and health cases.
- [ ] Add the Pet provider-side Pact verification test for the Appointment consumer Pact.
- [ ] Re-test `get_pet` and `get_owner_pets` through the real MCP server and Gateway after final integration changes.
- [ ] Write a short Pet Service section for the specification: bounded context, aggregates/value objects, database ownership, endpoints, authorization, and the verification contract.
- [ ] Prepare a 30–45 second explanation of Pet Service for the final video.

## Member 2 — Appointment Service owner

### Already complete

- [x] Appointment Service with Domain, Application, Infrastructure, and API layers.
- [x] Clinic, Veterinarian, AvailabilitySlot, and Appointment domain logic.
- [x] Scheduling, cancellation, rescheduling, availability, and upcoming-appointment use cases.
- [x] PostgreSQL persistence, migrations, and demo seed data.
- [x] REST API, Swagger/OpenAPI, health check, JWT authentication, and role authorization.
- [x] Kafka producers for `AppointmentScheduled`, `AppointmentCancelled`, and `AppointmentRescheduled`.
- [x] Consumer-side Pact tests for Pet verification.
- [x] Appointment MCP tools: `find_available_veterinarians` and `get_upcoming_appointments`.
- [x] Appointment Service registration in Consul.
- [x] Appointment domain and application tests.

### Remaining Member 2 work

- [ ] Fix Appointment → Pet verification to call the actual Pet route. Replace `/api/pets/{id}/exists` with the agreed canonical route, currently `/pets/{id}/exists`, and update the Pact interaction to match.
- [ ] Disable `PetService:UseFakeVerification` in Docker Compose and prove that appointment creation uses the real Pet Service.
- [ ] Replace `LocalServiceAccessTokenProvider` with a real Keycloak client-credentials token provider. Cache the access token until shortly before expiry and attach it through `ServiceAccessTokenHandler`.
- [ ] Add `ServiceDiscoveryHandler` to the Pet `HttpClient` so Pet Service is resolved through Consul instead of only through a fixed Docker address.
- [ ] Coordinate the Keycloak confidential service client, secret, service role, and Pet Service authorization policy with the team.
- [ ] Fix all 23 failing Appointment API integration tests. The current factory registers both Npgsql and EF InMemory providers; remove the production EF registrations before adding the test provider, or use PostgreSQL Testcontainers.
- [ ] Remove the mixed `Microsoft.IdentityModel.*` version warnings by aligning package versions.
- [ ] Run the Appointment API, domain, application, and consumer Pact suites and confirm that all are green.
- [ ] Verify real Kafka event publication for schedule, cancel, and reschedule operations.
- [ ] Re-test Appointment MCP tools through the real MCP server and Gateway.
- [ ] Write the Appointment Service section for the specification: domain rules, Pet ACL, Consul discovery, Keycloak client credentials, Kafka envelope, and API.
- [ ] Prepare a 30–45 second explanation of Appointment Service for the final video.

## Member 3 — Treatment, Notification, MCP, and Gateway owner

### Already complete

- [x] Treatment & Notification Service with Domain, Application, Infrastructure, and API layers.
- [x] Medical examination, vaccination, and notification domain logic.
- [x] PostgreSQL persistence, repositories, mappings, migrations, and demo data.
- [x] Medical examination, vaccination, medical history, vaccination history, and next-vaccination API flows.
- [x] Kafka consumer for appointment scheduled, cancelled, and rescheduled events.
- [x] Idempotency through `SourceEventId`, manual offset commit after success, retry, and error handling.
- [x] Notification background worker, scheduler, and demo console sender.
- [x] Keycloak JWT authentication and veterinarian/admin authorization.
- [x] Treatment domain tests, API integration tests, Kafka processing tests, health tests, and Pact tests.
- [x] Shared MCP server, Streamable HTTP transport, authentication, all nine registered tools, and integration tests.
- [x] Treatment MCP tools, including read and record operations.
- [x] YARP API Gateway routes, token forwarding, security behavior, MCP routing, and 30 Gateway tests.
- [x] Real Keycloak → Gateway → Treatment and Keycloak → Gateway → MCP → Treatment flows.
- [x] Repeatable verification script: `scripts/verify-gateway-treatment-mcp.sh`.

### Member 3 completion verification

- [x] Add focused worker tests for due notifications, successful console delivery, failed delivery, cancellation, and safe status transitions.
- [x] Verify the Treatment consumer against real Kafka and PostgreSQL through integration tests, including duplicate replay and dead-letter continuation.
- [x] Document the Treatment bounded context, notification worker, Kafka long polling, offset commit, retry, dead-lettering, and idempotency.
- [x] Document MCP/Gateway responsibilities, Streamable HTTP, bearer-token forwarding, YARP routing, and GitHub Copilot configuration.
- [x] Prepare the Treatment, Kafka, MCP, and Gateway speaking/demo notes for the final video.
- [x] Run all Member 3 suites: **147 passed, 0 failed**.
- [x] Rebuild the Docker containers and pass Keycloak → Gateway → Treatment and Keycloak → Gateway → MCP → Treatment verification.
- [x] Create and pass a live worker verification that console-delivers a due notification and persists `Sent`.
- [x] Remove the Treatment container's missing GSSAPI-library and HTTP-only HTTPS-redirection warnings.

**Member 3's implementation is complete.** Re-running these tests after later changes by Members 1 and 2 is a shared final regression task, not missing Member 3 functionality.

### Explicitly out of scope for Member 3

- **OUT OF SCOPE:** `GetPendingNotifications` API/query.
- **OUT OF SCOPE:** real email provider.
- **OUT OF SCOPE:** real SMS provider.
- **OUT OF SCOPE:** email/SMS placeholder implementations.

The console notification sender is the final delivery mechanism for this course project. The specification and presentation must describe it as a deliberate demo implementation, not as unfinished email/SMS work.

## Shared team tasks

### 1. Cross-service integration — highest priority

- [ ] Agree on and use one Pet verification route in Pet Service, Appointment Service, and Pact tests.
- [ ] Register Pet Service in Consul and make Appointment resolve it using Consul.
- [ ] Configure a real Keycloak client-credentials flow for Appointment → Pet.
- [ ] Remove the fake Pet verification setting from Docker Compose.
- [ ] Run this real main flow through the Gateway: authenticate → create/read owner and pet → find an available slot → schedule appointment → publish Kafka event → create and console-send notification → read treatment data.
- [ ] Test an invalid ownership case and confirm appointment scheduling is rejected.
- [ ] Test anonymous access, insufficient roles, valid owner actions, valid veterinarian actions, and service-to-service authorization.
- [ ] Confirm all Docker health checks become healthy and that Consul shows the expected registered services.

### 2. Automated quality checks

- [ ] Make the entire solution test run green. The 14 August 2026 full run produced **201 passed and 23 failed**; every failure is in Appointment API integration tests because their test host registers Npgsql and EF InMemory together.
- [ ] Add the missing Pet Service test projects and coverage.
- [ ] Complete both sides of the consumer-driven contract: Appointment consumer Pact and Pet provider verification.
- [ ] Keep Gateway, Treatment, MCP, Treatment Pact, Appointment domain/application, and Appointment Pact tests green.
- [ ] Save the final test commands and expected results in the root README.

### 3. Architecture diagram — mandatory course deliverable

- [ ] Create one architecture diagram containing:
  - client/demo caller and GitHub Copilot MCP client;
  - Keycloak;
  - YARP API Gateway;
  - Pet, Appointment, and Treatment & Notification services;
  - the MCP server;
  - one PostgreSQL database per service;
  - Consul service registry/discovery;
  - Kafka broker and the Appointment → Treatment event flow;
  - synchronous REST/ACL calls and authentication/token flow.
- [ ] Export the diagram in a format suitable for the specification and README, preferably SVG/PNG plus its editable source.
- [ ] Verify the diagram matches the implementation; do not show discovery or communication paths that are still faked.

### 4. Project specification — mandatory, maximum 10 pages

- [ ] Produce a polished final specification of no more than 10 pages.
- [ ] Cover the problem, architecture, three bounded contexts, service responsibilities, databases, configuration, REST/ACL, Kafka, Consul, Gateway, Keycloak, Pact, MCP, tests, and architecture diagram.
- [ ] Explain **how and why** each concept is implemented, not only which technology was selected.
- [ ] Correct stale claims in the current README before reusing its text. In particular, do not claim real Consul-based Pet discovery, real client credentials, or provider Pact verification until those tasks pass.
- [ ] Remove references to missing files or create them where useful, such as the final demo script and video script.
- [ ] Add each member’s ownership and contribution.

### 5. Final demonstration and video — mandatory

- [ ] Create a deterministic demo checklist/script with known users and seeded IDs.
- [ ] Demonstrate the three-service main flow, security, Kafka notification creation, and MCP tools.
- [ ] Demonstrate MCP through GitHub Copilot or another MCP-compatible client, not only an HTTP request.
- [ ] Write a video script and divide speaking responsibilities among all three members.
- [ ] Record a video of **at most five minutes** with audio explanation.
- [ ] Upload it to YouTube, Vimeo, Google Drive, or another accessible service and submit the link.

### 6. Final cleanup and submission

- [ ] Update the root README with prerequisites, ports, startup commands, Keycloak demo users, test commands, MCP client configuration, and the main demo flow.
- [ ] Remove or clearly label stale comments, fake-development notes, and claims that no longer match the code.
- [ ] Check that no real secrets or personal tokens are committed; keep only clearly marked demo credentials.
- [ ] Start from a clean clone, run Docker Compose, run tests, and perform the demo once before submission.
- [ ] Confirm the repository contains exactly three business microservices for the three-member team, a functional MCP server, Gateway, Consul, Keycloak, Kafka, Pact tests, architecture diagram, specification, and video link.

## Recommended execution order

1. Member 1 registers Pet Service in Consul and starts Pet tests/provider Pact work.
2. Member 2 fixes the Pet path, real Keycloak client credentials, Consul resolution, and the 23 failing tests.
3. Members 1 and 2 prove real Appointment → Pet communication and both sides of the Pact.
4. Member 3 and Member 2 prove the real Kafka producer → consumer → console notification flow.
5. Run the full system and all test suites until green.
6. Create the architecture diagram and final specification from the verified implementation.
7. Rehearse, record, and upload the five-minute video.

## Deadline

The course deadline in the supplied requirements is **15 September 2026**. Late submissions from October through January may receive at most 70% of the project points.
