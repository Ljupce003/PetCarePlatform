# PetCare Platform -- Team Task Breakdown

## Overview

This checklist is organized by owner. Each microservice owner is
responsible for the complete DDD stack (Domain, Application,
Infrastructure, API, Tests) for their bounded context. Shared
infrastructure is completed together.

------------------------------------------------------------------------

# Member 1 -- Pet Service

## 1. Project setup

-   [ ] Create Pet Service project
-   [ ] Configure dependency injection
-   [ ] Configure EF Core + PostgreSQL

## 2. Domain

### Owner Aggregate

-   [ ] Create Owner entity
-   [ ] Define invariants

### Pet Aggregate

-   [ ] Create Pet entity
-   [ ] Create MicrochipNumber Value Object
-   [ ] Add validation:
    -   [ ] Required name
    -   [ ] Valid birth date
    -   [ ] Valid microchip
    -   [ ] Allergies
    -   [ ] Chronic conditions

## 3. Application

-   [ ] Commands
-   [ ] Queries
-   [ ] DTOs
-   [ ] Validators
-   [ ] Mapping

## 4. Infrastructure

-   [ ] Repositories
-   [ ] DbContext
-   [ ] Migrations / EnsureCreated
-   [ ] Seed data

## 5. API

-   [ ] CRUD Owners
-   [ ] CRUD Pets
-   [ ] GET Pet
-   [ ] GET Owner Pets
-   [ ] GET /api/pets/{id}/exists?ownerId=...

## 6. Security

-   [ ] JWT Authentication
-   [ ] Owner/Admin authorization

## 7. Testing

-   [ ] Unit tests
-   [ ] Provider Pact test
-   [ ] Health endpoint

------------------------------------------------------------------------

# Member 2 -- Appointment Service

## 1. Project setup

-   [ ] Create service
-   [ ] Configure EF Core
-   [ ] Configure PostgreSQL

## 2. Domain

-   [ ] Clinic
-   [ ] Veterinarian
-   [ ] AvailabilitySlot
-   [ ] Appointment
-   [ ] Appointment state transitions
-   [ ] Booking rules
-   [ ] Reschedule rules
-   [ ] Cancel rules

## 3. Application

-   [ ] Schedule command
-   [ ] Cancel command
-   [ ] Reschedule command
-   [ ] Search available vets
-   [ ] Search slots

## 4. Infrastructure

-   [ ] DbContext
-   [ ] Repositories
-   [ ] Seed clinics
-   [ ] Seed vets
-   [ ] Seed slots

## 5. REST Integration

-   [ ] IPetVerificationClient
-   [ ] PetServiceClient
-   [ ] ACL mapping
-   [ ] Consul discovery
-   [ ] Client Credentials authentication

## 6. Kafka Producer

-   [ ] AppointmentScheduled
-   [ ] AppointmentCancelled
-   [ ] AppointmentRescheduled

## 7. API

-   [ ] Search endpoints
-   [ ] Booking endpoint
-   [ ] Cancel endpoint
-   [ ] Reschedule endpoint

## 8. Security

-   [ ] JWT
-   [ ] Role authorization

## 9. Testing

-   [ ] Consumer Pact tests
-   [ ] Health endpoint

------------------------------------------------------------------------

# Member 3 -- Treatment & Notification + MCP

## 1. Project setup

-   [ ] Create service
-   [ ] Configure EF Core
-   [ ] Configure PostgreSQL

## 2. Domain

-   [ ] MedicalExamination
-   [ ] Vaccination
-   [ ] Notification
-   [ ] Notification status
-   [ ] SourceEventId idempotency

## 3. Application

-   [ ] Add examination
-   [ ] Add vaccination
-   [ ] Medical history query
-   [ ] Next vaccination query

## 4. Infrastructure

-   [ ] DbContext
-   [ ] Repositories
-   [ ] Seed data

## 5. Kafka Consumer

-   [ ] Consume Scheduled
-   [ ] Consume Cancelled
-   [ ] Consume Rescheduled
-   [ ] Offset commit after success

## 6. Notification Worker

-   [ ] Background worker
-   [ ] Console notification sender
-   [ ] TODO Email/SMS

## 7. MCP Server

-   [ ] Configure SDK
-   [ ] Streamable HTTP
-   [ ] GetPet
-   [ ] GetUpcomingAppointments
-   [ ] GetNextVaccination
-   [ ] FindAvailableVeterinarians
-   [ ] API Gateway integration
-   [ ] Service account authentication

## 8. Security

-   [ ] Veterinarian/Admin authorization

## 9. Testing

-   [ ] MCP Inspector
-   [ ] Health endpoint

------------------------------------------------------------------------

# Shared Tasks

## Infrastructure

-   [ ] Docker Compose
-   [ ] API Gateway (YARP)
-   [ ] Consul
-   [ ] Kafka
-   [ ] Keycloak
-   [ ] Three PostgreSQL databases

## Security

-   [ ] Realm
-   [ ] Roles
-   [ ] Clients
-   [ ] JWT validation

## Documentation

-   [ ] Architecture diagram
-   [ ] Specification
-   [ ] README
-   [ ] API documentation

## Testing

-   [ ] End-to-end demo
-   [ ] Integration tests
-   [ ] Health checks

## Presentation

-   [ ] Demo script
-   [ ] 5-minute video
-   [ ] MCP demonstration
