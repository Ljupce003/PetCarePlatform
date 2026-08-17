# PetCare Platform -- Team Task Breakdown

The main idea is:

* Each member owns **their bounded context completely** (Domain → Application → Infrastructure → API → Tests).
* The **MCP server is a shared integration**, where one member owns the infrastructure and each service owner contributes their own tools.
* Shared infrastructure has clear primary owners to avoid duplicated work.

---

# PetCare Platform – Complete Team Task Breakdown

## Member 1 — Pet Service

### 1. Project Setup

* [ ] Create Pet Service solution/project
* [ ] Configure Clean Architecture projects

  * [ ] Domain
  * [ ] Application
  * [ ] Infrastructure
  * [ ] API
* [ ] Configure Dependency Injection
* [ ] Configure appsettings
* [ ] Configure logging
* [ ] Configure Swagger
* [ ] Configure EF Core
* [ ] Configure PostgreSQL connection
* [ ] Configure Health Checks

### 2. Domain Layer

#### Owner Aggregate

* [ ] Create Owner entity
* [ ] OwnerId
* [ ] OwnerName
* [ ] Email
* [ ] Phone
* [ ] Address
* [ ] Owner invariants

#### Pet Aggregate

* [ ] Create Pet entity
* [ ] PetId
* [ ] Name
* [ ] Species
* [ ] Breed
* [ ] BirthDate
* [ ] Weight
* [ ] Allergies
* [ ] ChronicConditions
* [ ] OwnerId reference

##### Value Objects

* [ ] MicrochipNumber
* [ ] PetName
* [ ] Breed (optional VO)

##### Domain Validation

* [ ] Required name
* [ ] Birth date validation
* [ ] Microchip validation
* [ ] Species validation
* [ ] Weight validation

##### Domain Exceptions

* [ ] PetAlreadyExists
* [ ] InvalidMicrochip
* [ ] InvalidBirthDate
* [ ] OwnerNotFound


### 3. Application Layer

##### Commands

* [ ] CreateOwner
* [ ] UpdateOwner
* [ ] DeleteOwner
* [ ] RegisterPet
* [ ] UpdatePet
* [ ] DeletePet

##### Queries

* [ ] GetPetById
* [ ] GetAllPets
* [ ] GetOwner
* [ ] GetOwnerPets
* [ ] CheckPetOwnership

##### DTOs

* [ ] PetDto
* [ ] OwnerDto
* [ ] CreatePetRequest
* [ ] UpdatePetRequest

##### Validators

* [ ] FluentValidation validators
* [ ] Request validation

##### Mapping

* [ ] Entity → DTO
* [ ] DTO → Entity

### 4. Infrastructure

* [ ] DbContext
* [ ] Entity configurations
* [ ] Repository interfaces
* [ ] Repository implementations
* [ ] EF migrations
* [ ] Seed owners
* [ ] Seed pets

### 5. API

##### Owner Endpoints

* [ ] POST Owner
* [ ] GET Owner
* [ ] GET Owners
* [ ] PUT Owner
* [ ] DELETE Owner

##### Pet Endpoints

* [ ] POST Pet
* [ ] GET Pet
* [ ] GET Pets
* [ ] PUT Pet
* [ ] DELETE Pet

##### Integration Endpoints

* [ ] GET Owner Pets
* [ ] GET Pet Exists
* [ ] GET Health


### 6. Security

* [ ] JWT authentication
* [ ] Owner role
* [ ] Admin role
* [ ] Authorization policies

### 7. MCP Contribution

Implement Pet-related MCP tools.

* [x] Define MCP contract for Pet tools
* [x] Implement `GetPet`
* [x] Implement `GetOwnerPets`
* [x] Test MCP tool responses

### 8. Testing

* [x] Unit tests
* [x] Repository tests
* [x] API tests
* [x] Provider Pact tests
* [x] Health endpoint test

---

# Member 2 — Appointment Service

### 1. Project Setup

* [ ] Create Appointment Service
* [ ] Configure Clean Architecture
* [ ] Configure EF Core
* [ ] Configure PostgreSQL
* [ ] Configure Swagger
* [ ] Configure Health Checks


### 2. Domain

#### Clinic

* [ ] Entity
* [ ] Validation

#### Veterinarian

* [ ] Entity
* [ ] Specialization
* [ ] Availability

#### AvailabilitySlot

* [ ] Entity
* [ ] Rules

#### Appointment

* [ ] Entity
* [ ] Status
* [ ] Date
* [ ] Duration

##### Business Rules

* [ ] Booking rules
* [ ] Conflict detection
* [ ] Double-book prevention
* [ ] Cancellation rules
* [ ] Reschedule rules
* [ ] Appointment state transitions


### 3. Application

##### Commands

* [ ] Schedule Appointment
* [ ] Cancel Appointment
* [ ] Reschedule Appointment

##### Queries

* [ ] Search Clinics
* [ ] Search Veterinarians
* [ ] Search Available Slots
* [ ] Upcoming Appointments

##### DTOs

* [ ] Appointment DTOs
* [ ] Clinic DTOs
* [ ] Vet DTOs

##### Validation

* [ ] Command validators
* [ ] Query validators

### 4. Infrastructure

* [ ] DbContext
* [ ] Repositories
* [ ] Entity configurations
* [ ] Seed Clinics
* [ ] Seed Veterinarians
* [ ] Seed Slots
* [ ] Migrations

### 5. REST Integration

* [ ] IPetVerificationClient
* [ ] HttpClient
* [ ] Consul service discovery
* [ ] ACL mapping
* [ ] Retry policy
* [ ] Client Credentials authentication

### 6. Kafka

##### Producer

* [ ] AppointmentScheduled
* [ ] AppointmentCancelled
* [ ] AppointmentRescheduled

### 7. API

* [ ] Search Clinics
* [ ] Search Vets
* [ ] Search Slots
* [ ] Book Appointment
* [ ] Cancel Appointment
* [ ] Reschedule Appointment
* [ ] Health endpoint

### 8. Security

* [ ] JWT
* [ ] Owner authorization
* [ ] Veterinarian authorization
* [ ] Admin authorization

### 9. MCP Contribution

Implement Appointment-related MCP tools.

* [x] Define MCP contract for Appointment tools
* [x] Implement `FindAvailableVeterinarians`
* [x] Implement `GetUpcomingAppointments`
* [x] Test MCP tool responses


### 10. Testing

* [ ] Unit tests
* [ ] Consumer Pact tests
* [ ] Integration tests
* [ ] Health tests

---

# Member 3 — Treatment Service + Notification + MCP Infrastructure

### 1. Project Setup

* [x] Create Treatment Service
* [x] Configure Clean Architecture
* [x] Configure EF Core
* [x] Configure PostgreSQL
* [x] Configure Swagger
* [x] Configure Health Checks

### 2. Domain

#### Medical Examination

* [x] Entity
* [x] Diagnosis
* [x] Notes

#### Vaccination

* [x] Entity
* [x] Vaccine
* [x] Date
* [x] Next Due Date

#### Notification

* [x] Entity
* [x] Status
* [x] Scheduled Time
* [x] Delivery State

##### Domain Rules

* [x] Vaccination scheduling
* [x] Notification status transitions
* [x] SourceEventId idempotency

---

### 3. Application

##### Commands

* [x] Add Examination
* [x] Add Vaccination
* [x] Create Notification

##### Queries

* [x] Medical History
* [x] Vaccination History
* [x] Next Vaccination
* **Out of scope:** Pending Notifications query

### 4. Infrastructure

* [x] DbContext
* [x] Repositories
* [x] Seed Data
* [x] Entity configurations


### 5. Kafka

##### Consumer

* [x] Consume AppointmentScheduled
* [x] Consume AppointmentCancelled
* [x] Consume AppointmentRescheduled
* [x] Idempotency handling
* [x] Offset commit after success
* [x] Error handling and dead-letter topic
* [x] Retry policy


### 6. Notification Worker

* [x] Background Worker
* [x] Notification Scheduler
* [x] Console Notification Sender
* **Out of scope:** Email provider/placeholder
* **Out of scope:** SMS provider/placeholder


### 7. API

* [x] Add Examination
* [x] Add Vaccination
* [x] Get Medical History
* [x] Get Vaccinations
* [x] Get Next Vaccination
* [x] Health endpoint


### 8. MCP Infrastructure

Own the shared MCP server.

* [x] Create MCP Server project
* [x] Configure MCP SDK
* [x] Configure Streamable HTTP transport
* [x] Configure dependency injection
* [x] Configure service authentication
* [x] Configure API Gateway integration
* [x] Register all MCP tools
* [x] Test MCP server with Inspector

> **Note:** The implementations of the service-specific tools (`GetPet`, `FindAvailableVeterinarians`, etc.) are contributed by the respective service owners. Member 3 is responsible for the MCP server infrastructure and integrating those tools into the server.


### 9. Security

* [x] JWT/Keycloak validation
* [x] Veterinarian authorization
* [x] Admin authorization


### 10. Testing

* [x] Unit tests
* [x] Kafka integration tests
* [x] Worker tests
* [x] MCP Inspector tests
* [x] Health tests

---

# Shared Infrastructure

## Member 1 (Primary Owner) – Security

### Keycloak

* [x] Create Realm
* [x] Create Roles
* [x] Create Clients
* [x] Configure Client Credentials
* [x] Configure JWT validation
* [x] Document authentication flow

---

## Member 2 (Primary Owner) – Infrastructure

### Docker Compose

* [ ] PostgreSQL containers
* [ ] Kafka
* [ ] Zookeeper/KRaft
* [ ] Consul
* [x] Keycloak
* [ ] Three microservices
* [x] MCP Server
* [x] API Gateway

### Consul

* [ ] Service registration
* [ ] Health checks

---

## Member 3 (Primary Owner) – Gateway & Integration

### API Gateway (YARP)

* [x] Route configuration
* [x] Authentication forwarding
* **Not used by design:** Gateway routing uses stable Docker service DNS; Consul discovery is demonstrated by the Appointment → Pet integration.
* [x] MCP endpoint routing

---

# Documentation (Everyone)

* [ ] Update README for owned service
* [ ] Document REST endpoints
* [ ] Document architecture decisions
* [ ] Update sequence diagrams
* [ ] Add OpenAPI/Swagger documentation

---

# Final Team Integration

* [ ] Verify all services communicate correctly
* [ ] Verify REST communication
* [ ] Verify Kafka messaging
* [ ] Verify authentication
* [x] Verify MCP tools end-to-end
* [ ] Run end-to-end demo
* [ ] Fix integration issues
* [ ] Prepare presentation
* [ ] Record demo video
