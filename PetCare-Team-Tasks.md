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

* [ ] Define MCP contract for Pet tools
* [ ] Implement `GetPet`
* [ ] Implement `GetOwnerPets`
* [ ] Test MCP tool responses

### 8. Testing

* [ ] Unit tests
* [ ] Repository tests
* [ ] API tests
* [ ] Provider Pact tests
* [ ] Health endpoint test

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

* [ ] Define MCP contract for Appointment tools
* [ ] Implement `FindAvailableVeterinarians`
* [ ] Implement `GetUpcomingAppointments`
* [ ] Test MCP tool responses


### 10. Testing

* [ ] Unit tests
* [ ] Consumer Pact tests
* [ ] Integration tests
* [ ] Health tests

---

# Member 3 — Treatment Service + Notification + MCP Infrastructure

### 1. Project Setup

* [ ] Create Treatment Service
* [ ] Configure Clean Architecture
* [ ] Configure EF Core
* [ ] Configure PostgreSQL
* [ ] Configure Swagger
* [ ] Configure Health Checks

### 2. Domain

#### Medical Examination

* [ ] Entity
* [ ] Diagnosis
* [ ] Notes

#### Vaccination

* [ ] Entity
* [ ] Vaccine
* [ ] Date
* [ ] Next Due Date

#### Notification

* [ ] Entity
* [ ] Status
* [ ] Scheduled Time
* [ ] Delivery State

##### Domain Rules

* [ ] Vaccination scheduling
* [ ] Notification status transitions
* [ ] SourceEventId idempotency

---

### 3. Application

##### Commands

* [ ] Add Examination
* [ ] Add Vaccination
* [ ] Create Notification

##### Queries

* [ ] Medical History
* [ ] Vaccination History
* [ ] Next Vaccination
* [ ] Pending Notifications

### 4. Infrastructure

* [ ] DbContext
* [ ] Repositories
* [ ] Seed Data
* [ ] Entity configurations


### 5. Kafka

##### Consumer

* [ ] Consume AppointmentScheduled
* [ ] Consume AppointmentCancelled
* [ ] Consume AppointmentRescheduled
* [ ] Idempotency handling
* [ ] Offset commit after success
* [ ] Error handling
* [ ] Retry policy


### 6. Notification Worker

* [ ] Background Worker
* [ ] Notification Scheduler
* [ ] Console Notification Sender
* [ ] Email placeholder
* [ ] SMS placeholder


### 7. API

* [ ] Add Examination
* [ ] Add Vaccination
* [ ] Get Medical History
* [ ] Get Vaccinations
* [ ] Get Next Vaccination
* [ ] Health endpoint


### 8. MCP Infrastructure

Own the shared MCP server.

* [ ] Create MCP Server project
* [ ] Configure MCP SDK
* [ ] Configure Streamable HTTP transport
* [ ] Configure dependency injection
* [ ] Configure service authentication
* [ ] Configure API Gateway integration
* [ ] Register all MCP tools
* [ ] Test MCP server with Inspector

> **Note:** The implementations of the service-specific tools (`GetPet`, `FindAvailableVeterinarians`, etc.) are contributed by the respective service owners. Member 3 is responsible for the MCP server infrastructure and integrating those tools into the server.


### 9. Security

* [ ] JWT
* [ ] Veterinarian authorization
* [ ] Admin authorization


### 10. Testing

* [ ] Unit tests
* [ ] Kafka integration tests
* [ ] Worker tests
* [ ] MCP Inspector tests
* [ ] Health tests

---

# Shared Infrastructure

## Member 1 (Primary Owner) – Security

### Keycloak

* [ ] Create Realm
* [ ] Create Roles
* [ ] Create Clients
* [ ] Configure Client Credentials
* [ ] Configure JWT validation
* [ ] Document authentication flow

---

## Member 2 (Primary Owner) – Infrastructure

### Docker Compose

* [ ] PostgreSQL containers
* [ ] Kafka
* [ ] Zookeeper/KRaft
* [ ] Consul
* [ ] Keycloak
* [ ] Three microservices
* [ ] MCP Server
* [ ] API Gateway

### Consul

* [ ] Service registration
* [ ] Health checks

---

## Member 3 (Primary Owner) – Gateway & Integration

### API Gateway (YARP)

* [ ] Route configuration
* [ ] Authentication forwarding
* [ ] Service discovery integration
* [ ] MCP endpoint routing

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
* [ ] Verify MCP tools end-to-end
* [ ] Run end-to-end demo
* [ ] Fix integration issues
* [ ] Prepare presentation
* [ ] Record demo video
