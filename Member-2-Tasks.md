# Member 2 — Appointment Service Tasks

## Role in the project
You are the owner of the Appointment bounded context and the main implementation lead for the Appointment Service.
Your work must cover business logic, service discovery, service-to-service communication, Kafka events, API exposure, and integration testing.

## Primary responsibilities
- Implement the Appointment Service as an independent microservice.
- Own the appointment domain model, business rules, persistence, REST API, and tests.
- Integrate with the Pet Service using REST and service discovery.
- Publish integration events through Kafka.
- Support the shared MCP server with appointment-related tools.
- Document your service clearly for the final project submission.

---

## 1. Service setup and architecture
- [ ] Create the Appointment Service project structure.
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
- The service starts independently and exposes Swagger and health endpoints.
- Its architecture follows the project’s microservice pattern.

---

## 2. Domain-driven design for Appointment bounded context
You must model the appointment domain as a separate business context.

### Clinic entity
- [ ] Create the `Clinic` entity.
- [ ] Add relevant properties and validation.

### Veterinarian entity
- [ ] Create the `Veterinarian` entity.
- [ ] Add specialization and availability information.

### AvailabilitySlot entity
- [ ] Create the `AvailabilitySlot` entity.
- [ ] Define rules for slot validity and conflicts.
- [ ] Prevent double-booking and expired slots.

### Appointment entity
- [ ] Create the `Appointment` entity.
- [ ] Add fields for status, date, duration, clinic, veterinarian, and pet reference.
- [ ] Implement state transitions such as Scheduled → Cancelled/Completed.

### Business rules
- [ ] Implement booking rules.
- [ ] Implement conflict detection.
- [ ] Prevent double booking.
- [ ] Implement cancellation rules.
- [ ] Implement reschedule rules.
- [ ] Define allowed transitions for appointment states.

### Acceptance criteria
- The domain layer enforces appointment business rules without relying on the API layer.

---

## 3. Application layer
Implement use cases for appointment workflow.

### Commands
- [ ] Implement `ScheduleAppointment`
- [ ] Implement `CancelAppointment`
- [ ] Implement `RescheduleAppointment`

### Queries
- [ ] Implement `SearchClinics`
- [ ] Implement `SearchVeterinarians`
- [ ] Implement `SearchAvailableSlots`
- [ ] Implement `GetUpcomingAppointments`

### DTOs and validation
- [ ] Create appointment-related DTOs.
- [ ] Create clinic and veterinarian DTOs.
- [ ] Add command and query validators.
- [ ] Implement mapping between domain entities and DTOs.

### Acceptance criteria
- Appointment operations are exposed through well-defined application services.
- All key business actions are validated before persistence.

---

## 4. Infrastructure layer
- [ ] Create the EF Core `DbContext`.
- [ ] Configure entity mappings for clinics, veterinarians, slots, and appointments.
- [ ] Implement repository interfaces and concrete repositories.
- [ ] Add migrations.
- [ ] Seed clinics, veterinarians, and availability slots for demo data.
- [ ] Ensure the service uses its own PostgreSQL database.

### Acceptance criteria
- Appointment data is isolated in the Appointment Service database.
- Demo data allows the full workflow to be shown in the presentation.

---

## 5. REST integration with Pet Service
This is one of the most important parts of your work.

- [ ] Create an `IPetVerificationClient` abstraction.
- [ ] Implement an `HttpClient`-based client for the Pet Service.
- [ ] Add an anti-corruption layer so the Appointment Service uses a simplified model.
- [ ] Implement logic to verify pet ownership before booking.
- [ ] Add retry policy for transient failures.
- [ ] Use client-credentials authentication for service-to-service requests.
- [ ] Resolve the Pet Service address dynamically through Consul.

### Acceptance criteria
- Appointment booking depends on a verified pet ownership check.
- The Appointment Service can recover gracefully from temporary downstream failures.

---

## 6. Service discovery and infrastructure integration
- [ ] Register the Appointment Service in Consul.
- [ ] Configure health checks for the service instance.
- [ ] Ensure the service can resolve healthy instances at runtime.
- [ ] Document the service discovery flow for the architecture section.

### Acceptance criteria
- The service can be discovered by other components in the deployment environment.

---

## 7. Kafka integration events
Publish appointment lifecycle events for downstream processing.

### Producers
- [ ] Implement `AppointmentScheduled` event publishing.
- [ ] Implement `AppointmentCancelled` event publishing.
- [ ] Implement `AppointmentRescheduled` event publishing.
- [ ] Use an event envelope with event type, payload, timestamp, and correlation ID.
- [ ] Ensure the event publisher is reliable and idempotent where possible.

### Acceptance criteria
- Treatment & Notification Service can consume the appointment events successfully.
- Events contain enough information for downstream notifications.

---

## 8. REST API layer
Expose appointment functionality through REST endpoints.

- [ ] `GET /clinics`
- [ ] `GET /veterinarians`
- [ ] `GET /slots`
- [ ] `POST /appointments`
- [ ] `DELETE /appointments/{id}`
- [ ] `PUT /appointments/{id}/reschedule`
- [ ] `GET /health`

### Acceptance criteria
- The API supports the complete appointment workflow.
- Successful operations return clear responses.

---

## 9. Security and authorization
- [ ] Implement JWT authentication.
- [ ] Add role-based authorization for:
  - [ ] `owner`
  - [ ] `veterinarian`
  - [ ] `admin`
- [ ] Protect booking and rescheduling actions appropriately.
- [ ] Ensure service-to-service calls authenticate correctly.

### Acceptance criteria
- Unauthorized users cannot manipulate appointments.
- The service can authenticate to downstream services securely.

---

## 10. MCP contribution
You own the appointment-related tools in the shared MCP server.

- [ ] Define the MCP contract for appointment tools.
- [ ] Implement `FindAvailableVeterinarians`.
- [ ] Implement `GetUpcomingAppointments`.
- [ ] Test the MCP tool responses.

### Acceptance criteria
- The MCP server exposes appointment information through the registered tools.

---

## 11. Testing
- [ ] Write unit tests for appointment domain logic.
- [ ] Write integration tests for REST endpoints.
- [ ] Write consumer-side Pact tests for the Pet Service contract.
- [ ] Add health endpoint tests.
- [ ] Validate message production behavior where possible.

### Acceptance criteria
- The appointment workflow is covered by automated tests.
- The contract with Pet Service is verified.

---

## 12. Documentation and deliverables
- [ ] Document the appointment domain and business rules.
- [ ] Document the Pet Service integration flow.
- [ ] Document the Kafka event structure.
- [ ] Add Swagger/OpenAPI documentation for the API.
- [ ] Prepare the service explanation for the final presentation.

---

## Definition of done
You are done when:
- the Appointment Service is runnable as a microservice;
- appointments can be booked, cancelled, and rescheduled;
- pet verification works through the external integration layer;
- appointment events are produced correctly;
- tests and documentation are complete.
