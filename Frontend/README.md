# PetCare Frontend

A standalone React/Vite operations console for the PetCare microservices. It authenticates through Keycloak using Authorization Code + PKCE and calls the existing services exclusively through the YARP API Gateway.

## Run it

### Docker Compose (recommended)

From the repository root, run `docker compose up --build`. The `frontend` service is built with Node and served by Nginx at `http://localhost:5173`. Nginx proxies `/gateway/*` internally to `api-gateway`, so API traffic stays on the same browser origin.

### Local development

1. Start the platform (including Keycloak and the gateway) with the repository's normal Docker Compose workflow.
2. Copy `.env.example` to `.env` if you need to change any endpoint values.
3. From this directory, run `npm install` and `npm run dev`.
4. Open `http://localhost:5173` and use a Keycloak demo account, such as `owner1` / `Owner123!`, `vet1` / `Vet123!`, or `admin1` / `Admin123!`.

The Vite dev server proxies `/gateway/*` to `http://localhost:7000`, avoiding browser cross-origin issues in local development. The gateway also now permits the Vite origin for a deployed/separate frontend. For another host, set `Frontend:AllowedOrigins` in the gateway configuration and `VITE_API_BASE` to the gateway URL.

## Included workflows

- Keycloak sign-in/sign-out and role-aware controls.
- Owner and pet creation, editing, deletion, profile lookups, and ownership-backed appointment booking.
- Clinic, veterinarian, open-slot search, scheduling, cancellation, rescheduling, and administrator slot creation.
- Medical-examination and vaccination history plus clinician record creation.
- Owner notification listing and administrator-created notification support, alongside a visible Kafka/worker flow explanation.
