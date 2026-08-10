# Member 3 — Treatment, Notification, and MCP Infrastructure Tasks

## Role in the project
You are the owner of the Treatment & Notification bounded context and the shared MCP infrastructure.
Your responsibilities span the Treatment Service, Kafka event consumption, notification processing, the notification worker, and the overall MCP server integration.

## Primary responsibilities
- Implement the Treatment Service as an independent microservice.
- Own medical examination, vaccination, and notification domain logic.
- Consume appointment events from Kafka and generate notifications.
- Implement the shared MCP server infrastructure and register all tools.
- Support the API Gateway, security, and end-to-end integration testing.
- Document your work clearly for the final project submission.

---

## 1. Service setup and architecture
- [x] Create the Treatment Service project structure.
- [x] Configure Clean Architecture layers:
  - [x] Domain
  - [x] Application
  - [x] Infrastructure
  - [x] API
- [x] Configure dependency injection and service registration.
- [x] Configure logging, Swagger/OpenAPI, and health checks.
- [x] Configure EF Core with PostgreSQL.
- [x] Prepare Docker/service startup configuration.

### Acceptance criteria
- The Treatment Service starts independently and exposes Swagger and health endpoints.
- The architecture supports clear separation between domain logic and infrastructure.

---

## 2. Domain-driven design for Treatment & Notification bounded context
You must model the medical domain as a separate business context.

### MedicalExamination entity
- [x] Create the `MedicalExamination` entity.
- [x] Add fields such as diagnosis, notes, therapy, prescribed medication, and follow-up information.
- [x] Implement validation rules for required medical information.

### Vaccination entity
- [x] Create the `Vaccination` entity.
- [x] Add vaccine name, date administered, and next due date.
- [x] Implement rules for vaccination scheduling and due-date calculation.

### Notification entity
- [x] Create the `Notification` entity.
- [x] Add fields for status, scheduled time, and delivery state.
- [x] Implement status transitions for pending, sent, and failed notifications.
- [x] Add a `SourceEventId` to support idempotency.

### Domain rules
- [x] Implement vaccination scheduling rules.
- [x] Implement notification status transitions.
- [x] Prevent duplicate processing of the same event.

### Acceptance criteria
- Medical data is modeled independently of the other services.
- Notification handling is safe against duplicate Kafka delivery.

---

## 3. Application layer
Implement use cases for medical and notification flows.

### Commands
- [x] Implement `AddExamination`
- [x] Implement `AddVaccination`
- [x] Implement `CreateNotification`

### Queries
- [x] Implement `GetMedicalHistory`
- [x] Implement `GetVaccinationHistory`
- [x] Implement `GetNextVaccination`
- [ ] Implement `GetPendingNotifications`

### DTOs and validation
- [x] Create medical and notification DTOs.
- [x] Add validators for commands and queries.
- [x] Implement mapping between entities and DTOs.

### Acceptance criteria
- Treatment operations are exposed through application services.
- Validation and business rules are consistent.

---

## 4. Infrastructure layer
- [x] Create the EF Core `DbContext`.
- [x] Configure entity mappings for medical and notification data.
- [x] Implement repository interfaces and concrete repositories.
- [x] Add migrations.
- [x] Seed sample medical data and notifications for demonstration.
- [x] Ensure the service uses its own PostgreSQL database.

### Acceptance criteria
- Treatment data is isolated in the Treatment Service database.
- Demo data allows the notification workflow to be demonstrated.

---

## 5. Kafka consumer and event-driven workflow
You are responsible for processing appointment events produced by Appointment Service.

### Consumers
- [x] Consume `AppointmentScheduled` events.
- [x] Consume `AppointmentCancelled` events.
- [x] Consume `AppointmentRescheduled` events.
- [x] Implement idempotency handling with `SourceEventId`.
- [x] Commit offsets only after successful processing.
- [x] Add retry and error handling for failed processing.

### Acceptance criteria
- Notifications are created only once per event.
- Failed events do not cause permanent data inconsistency.

---

## 6. Notification worker
Implement the background processing part of the notification flow.

- [x] Create a background worker service.
- [x] Implement a scheduler for pending notifications.
- [x] Implement a demo console sender for notifications.
- [ ] Add placeholders for email and SMS delivery.
- [x] Ensure sending is safe and logged clearly.

### Acceptance criteria
- Notifications can be processed and emitted in the demo environment.
- The workflow is visible during the final presentation.

---

## 7. REST API layer
Expose treatment and notification functionality through REST endpoints.

- [x] `POST /examinations`
- [x] `POST /vaccinations`
- [x] `GET /medical-history`
- [x] `GET /vaccinations`
- [x] `GET /next-vaccination`
- [x] `GET /health`

### Acceptance criteria
- The API supports medical record and notification management.
- The service can be tested end-to-end with the rest of the platform.

---

## 8. Security and authorization
- [x] Implement JWT authentication.
- [x] Add authorization for:
  - [x] `veterinarian`
  - [x] `admin`
- [x] Protect medical data endpoints appropriately.
- [x] Ensure service-to-service authentication is configured correctly.

### Acceptance criteria
- Only authorized actors can create medical records and notifications.
- The service can be accessed securely through the gateway.

---

## 9. MCP server infrastructure
You own the shared MCP server in the architecture.

- [ ] Create the MCP Server project.
- [ ] Configure the MCP SDK.
- [ ] Configure Streamable HTTP transport.
- [ ] Configure dependency injection.
- [ ] Configure service authentication for MCP requests.
- [ ] Integrate the MCP server with the API Gateway.
- [ ] Register all tools from the service owners.
- [ ] Test the server with MCP Inspector or another MCP client.

### Acceptance criteria
- The MCP server can be started and accessed via the chosen transport.
- The shared server exposes the required Pet, Appointment, and Treatment tools.

---

## 10. Integration with gateway and shared infrastructure
- [ ] Ensure the Treatment Service is correctly routed through the API Gateway.
- [ ] Confirm the shared security model works for gateway and service access.
- [ ] Verify cross-service communication paths in the deployed environment.

### Acceptance criteria
- The full system is reachable through the gateway.
- MCP server integration works as part of the architecture.

---

## 11. Testing
- [x] Write unit tests for domain logic.
- [x] Write integration tests for Kafka processing.
- [ ] Write worker tests for notification processing.
- [ ] Add MCP Inspector or client-side tests.
- [x] Add health endpoint tests.

### Acceptance criteria
- Event processing and notification flow are covered by automated tests.
- The MCP server is tested in a realistic execution environment.

---

## 12. Documentation and deliverables
- [ ] Document the Treatment Service architecture and domain design.
- [ ] Document the Kafka consumer flow and notification model.
- [ ] Document the MCP server responsibilities and tool registration.
- [x] Add Swagger/OpenAPI documentation for the API.
- [ ] Prepare the service explanation for the final presentation.

---

## Definition of done
You are done when:
- the Treatment Service is runnable as a microservice;
- medical records, vaccinations, and notifications are implemented;
- Kafka event consumption and notification generation work correctly;
- the MCP server is implemented and reachable;
- tests and documentation are complete.
