# Member 1 — Pet Service Tasks

## Role in the project
You are the owner of the Pet bounded context and the primary implementation lead for the Pet Service.
Your service must be implemented as an independent microservice with clear boundaries, well-defined APIs, tests, and MCP integration.

## Primary responsibilities
- Implement the Pet Service as a standalone microservice.
- Own the Pet domain model, application logic, persistence, REST API, and tests.
- Provide the service-specific MCP tools used by the shared MCP server.
- Support cross-service integration with the Appointment Service.
- Document your service clearly for the final project submission.

---

## 1. Service setup and architecture
- [ ] Create the Pet Service project structure inside the monorepo.
- [ ] Configure Clean Architecture layers:
  - [ ] Domain
  - [ ] Application
  - [ ] Infrastructure
  - [ ] API
- [ ] Configure dependency injection for all layers.
- [ ] Configure application settings for local development and Docker usage.
- [ ] Configure logging and structured logging.
- [ ] Configure Swagger/OpenAPI and health checks.
- [ ] Configure EF Core with PostgreSQL.
- [ ] Prepare a Docker-compatible service configuration.

### Acceptance criteria
- The service can start locally and expose Swagger and health endpoints.
- The project structure follows Clean Architecture and keeps domain logic isolated.

---

## 2. Domain-driven design for Pet bounded context
You must model the Pet domain as a self-contained business domain.

### Owner aggregate
- [ ] Create the `Owner` entity.
- [ ] Add fields: `OwnerId`, `OwnerName`, `Email`, `Phone`, `Address`.
- [ ] Define invariants for owner data.
- [ ] Add validation rules for required fields and valid email/phone formats.

### Pet aggregate
- [ ] Create the `Pet` entity.
- [ ] Add fields: `PetId`, `Name`, `Species`, `Breed`, `BirthDate`, `Weight`, `Allergies`, `ChronicConditions`, `OwnerId`.
- [ ] Define a clear relationship between `Owner` and `Pet`.
- [ ] Implement business rules for pet creation and update.

### Value objects and domain rules
- [ ] Create or use value objects for `MicrochipNumber`, `PetName`, and optionally `Breed`.
- [ ] Enforce validation for:
  - [ ] Required pet name
  - [ ] Valid birth date
  - [ ] Valid microchip number format
  - [ ] Valid species
  - [ ] Valid weight
- [ ] Create domain exceptions for:
  - [ ] `PetAlreadyExists`
  - [ ] `InvalidMicrochip`
  - [ ] `InvalidBirthDate`
  - [ ] `OwnerNotFound`

### Acceptance criteria
- The domain layer contains business rules and does not depend on the API or infrastructure layers.
- Invalid pet data cannot be persisted.

---

## 3. Application layer
Implement use cases for the Pet domain.

### Commands
- [ ] Implement `CreateOwner`
- [ ] Implement `UpdateOwner`
- [ ] Implement `DeleteOwner`
- [ ] Implement `RegisterPet`
- [ ] Implement `UpdatePet`
- [ ] Implement `DeletePet`

### Queries
- [ ] Implement `GetPetById`
- [ ] Implement `GetAllPets`
- [ ] Implement `GetOwner`
- [ ] Implement `GetOwnerPets`
- [ ] Implement `CheckPetOwnership`

### DTOs and validation
- [ ] Create `PetDto`
- [ ] Create `OwnerDto`
- [ ] Create `CreatePetRequest`
- [ ] Create `UpdatePetRequest`
- [ ] Add FluentValidation validators for commands and requests.
- [ ] Implement object mapping between entities and DTOs.

### Acceptance criteria
- All business operations are reachable through application services.
- Validation errors are returned consistently.

---

## 4. Infrastructure layer
- [ ] Create the EF Core `DbContext`.
- [ ] Configure entity mappings for `Owner` and `Pet`.
- [ ] Implement repository interfaces and concrete repository classes.
- [ ] Add database migrations.
- [ ] Seed sample owners and pets for demo use.
- [ ] Ensure the service can work with a dedicated PostgreSQL database.

### Acceptance criteria
- Data is stored in the Pet Service database only.
- The service can run with seeded demo data.

---

## 5. REST API layer
You must expose the Pet Service through REST endpoints.

### Owner endpoints
- [ ] `POST /owners`
- [ ] `GET /owners/{id}`
- [ ] `GET /owners`
- [ ] `PUT /owners/{id}`
- [ ] `DELETE /owners/{id}`

### Pet endpoints
- [ ] `POST /pets`
- [ ] `GET /pets/{id}`
- [ ] `GET /pets`
- [ ] `PUT /pets/{id}`
- [ ] `DELETE /pets/{id}`

### Integration endpoints
- [ ] `GET /owners/{ownerId}/pets`
- [ ] `GET /pets/{id}/exists?ownerId=...`
- [ ] `GET /health`

### Acceptance criteria
- Endpoints return correct status codes and meaningful payloads.
- The integration endpoint used by Appointment Service works correctly.

---

## 6. Security and authorization
- [ ] Implement JWT authentication for the Pet Service.
- [ ] Add role-based authorization for:
  - [ ] `owner`
  - [ ] `admin`
- [ ] Enforce authorization policies on owner and pet endpoints.
- [ ] Ensure the service can validate tokens issued by Keycloak.

### Acceptance criteria
- Protected endpoints reject unauthorized requests.
- The service uses the same token validation approach as the rest of the system.

---

## 7. Cross-service integration work
The Appointment Service must be able to verify pet ownership without directly accessing your domain model.

- [ ] Design a stable anti-corruption interface for pet verification.
- [ ] Expose a simple response contract that Appointment Service can consume.
- [ ] Ensure the endpoint returns only the information required by the consumer.
- [ ] Document how the Pet Service is used by Appointment Service.

### Acceptance criteria
- Appointment Service can call the Pet Service safely without sharing internal domain objects.

---

## 8. MCP contribution
You own the Pet-related tools in the shared MCP server.

- [ ] Define the MCP contract for Pet tools.
- [ ] Implement `GetPet`.
- [ ] Implement `GetOwnerPets`.
- [ ] Test MCP tool responses with a local client or inspector.

### Acceptance criteria
- The MCP server can return Pet Service data through the registered tools.

---

## 9. Testing
- [ ] Write unit tests for domain logic.
- [ ] Write repository tests for persistence behavior.
- [ ] Write API tests for all main endpoints.
- [ ] Add provider-side Pact tests for the Pet Service contract.
- [ ] Add a health endpoint test.

### Acceptance criteria
- Core business flows and API behavior are covered by automated tests.
- Contracts between Pet Service and Appointment Service are verified.

---

## 10. Documentation and deliverables
- [ ] Document the Pet Service architecture and responsibilities.
- [ ] Add endpoint documentation in Swagger/OpenAPI.
- [ ] Document the domain model and business rules.
- [ ] Prepare a short summary of how the Pet Service works for the final presentation.

---

## Definition of done
You are done when:
- the Pet Service is runnable as a microservice;
- the domain, application, infrastructure, and API layers are implemented;
- the service is secured and integrated with the wider system;
- MCP Pet tools work correctly;
- tests and documentation are complete.
