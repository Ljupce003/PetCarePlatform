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
- [ ] Create the Treatment Service project structure.
- [ ] Configure Clean Architecture layers:
  - [ ] Domain
  - [ ] Application
  - [ ] Infrastructure
  - [ ] API
- [ ] Configure dependency injection and service registration.
- [ ] Configure logging, Swagger/OpenAPI, and health checks.
- [ ] Configure EF Core with PostgreSQL.
- [ ] Prepare Docker/service startup configuration.

### Acceptance criteria
- The Treatment Service starts independently and exposes Swagger and health endpoints.
- The architecture supports clear separation between domain logic and infrastructure.

---

## 2. Domain-driven design for Treatment & Notification bounded context
You must model the medical domain as a separate business context.

### MedicalExamination entity
- [ ] Create the `MedicalExamination` entity.
- [ ] Add fields such as diagnosis, notes, therapy, prescribed medication, and follow-up information.
- [ ] Implement validation rules for required medical information.

### Vaccination entity
- [ ] Create the `Vaccination` entity.
- [ ] Add vaccine name, date administered, and next due date.
- [ ] Implement rules for vaccination scheduling and due-date calculation.

### Notification entity
- [ ] Create the `Notification` entity.
- [ ] Add fields for status, scheduled time, and delivery state.
- [ ] Implement status transitions for pending, sent, and failed notifications.
- [ ] Add a `SourceEventId` to support idempotency.

### Domain rules
- [ ] Implement vaccination scheduling rules.
- [ ] Implement notification status transitions.
- [ ] Prevent duplicate processing of the same event.

### Acceptance criteria
- Medical data is modeled independently of the other services.
- Notification handling is safe against duplicate Kafka delivery.

---

## 3. Application layer
Implement use cases for medical and notification flows.

### Commands
- [ ] Implement `AddExamination`
- [ ] Implement `AddVaccination`
- [ ] Implement `CreateNotification`

### Queries
- [ ] Implement `GetMedicalHistory`
- [ ] Implement `GetVaccinationHistory`
- [ ] Implement `GetNextVaccination`
- [ ] Implement `GetPendingNotifications`

### DTOs and validation
- [ ] Create medical and notification DTOs.
- [ ] Add validators for commands and queries.
- [ ] Implement mapping between entities and DTOs.

### Acceptance criteria
- Treatment operations are exposed through application services.
- Validation and business rules are consistent.

---

## 4. Infrastructure layer
- [ ] Create the EF Core `DbContext`.
- [ ] Configure entity mappings for medical and notification data.
- [ ] Implement repository interfaces and concrete repositories.
- [ ] Add migrations.
- [ ] Seed sample medical data and notifications for demonstration.
- [ ] Ensure the service uses its own PostgreSQL database.

### Acceptance criteria
- Treatment data is isolated in the Treatment Service database.
- Demo data allows the notification workflow to be demonstrated.

---

## 5. Kafka consumer and event-driven workflow
You are responsible for processing appointment events produced by Appointment Service.

### Consumers
- [ ] Consume `AppointmentScheduled` events.
- [ ] Consume `AppointmentCancelled` events.
- [ ] Consume `AppointmentRescheduled` events.
- [ ] Implement idempotency handling with `SourceEventId`.
- [ ] Commit offsets only after successful processing.
- [ ] Add retry and error handling for failed processing.

### Acceptance criteria
- Notifications are created only once per event.
- Failed events do not cause permanent data inconsistency.

---

## 6. Notification worker
Implement the background processing part of the notification flow.

- [ ] Create a background worker service.
- [ ] Implement a scheduler for pending notifications.
- [ ] Implement a demo console sender for notifications.
- [ ] Add placeholders for email and SMS delivery.
- [ ] Ensure sending is safe and logged clearly.

### Acceptance criteria
- Notifications can be processed and emitted in the demo environment.
- The workflow is visible during the final presentation.

---

## 7. REST API layer
Expose treatment and notification functionality through REST endpoints.

- [ ] `POST /examinations`
- [ ] `POST /vaccinations`
- [ ] `GET /medical-history`
- [ ] `GET /vaccinations`
- [ ] `GET /next-vaccination`
- [ ] `GET /health`

### Acceptance criteria
- The API supports medical record and notification management.
- The service can be tested end-to-end with the rest of the platform.

---

## 8. Security and authorization
- [ ] Implement JWT authentication.
- [ ] Add authorization for:
  - [ ] `veterinarian`
  - [ ] `admin`
- [ ] Protect medical data endpoints appropriately.
- [ ] Ensure service-to-service authentication is configured correctly.

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
- [ ] Write unit tests for domain logic.
- [ ] Write integration tests for Kafka processing.
- [ ] Write worker tests for notification processing.
- [ ] Add MCP Inspector or client-side tests.
- [ ] Add health endpoint tests.

### Acceptance criteria
- Event processing and notification flow are covered by automated tests.
- The MCP server is tested in a realistic execution environment.

---

## 12. Documentation and deliverables
- [ ] Document the Treatment Service architecture and domain design.
- [ ] Document the Kafka consumer flow and notification model.
- [ ] Document the MCP server responsibilities and tool registration.
- [ ] Add Swagger/OpenAPI documentation for the API.
- [ ] Prepare the service explanation for the final presentation.

---

## Definition of done
You are done when:
- the Treatment Service is runnable as a microservice;
- medical records, vaccinations, and notifications are implemented;
- Kafka event consumption and notification generation work correctly;
- the MCP server is implemented and reachable;
- tests and documentation are complete.
