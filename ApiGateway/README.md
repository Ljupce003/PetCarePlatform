# PetCare API Gateway

The Gateway is the public HTTP entry point for the PetCare services. YARP validates a Keycloak JWT before forwarding the original bearer token to the selected service.

| Public path | Destination |
| --- | --- |
| `/pet/{**path}` | Pet Service, with `/pet` removed |
| `/appointment/{**path}` | Appointment Service, with `/appointment` removed |
| `/treatment/{**path}` | Treatment & Notification Service, with `/treatment` removed |
| `/mcp` | Shared MCP Server, with the MCP path preserved |

`/health` is intentionally anonymous. Every proxy route uses the default authorization policy.

## Unified OpenAPI and Swagger UI

Start the Docker stack and open:

- Swagger UI: `http://localhost:7000/swagger`
- Machine-readable catalog: `http://localhost:7000/openapi`
- Pet OpenAPI: `http://localhost:7000/openapi/pet.json`
- Appointment OpenAPI: `http://localhost:7000/openapi/appointment.json`
- Treatment OpenAPI: `http://localhost:7000/openapi/treatment.json`

Visiting `http://localhost:7000/` or `/docs` redirects to the unified Swagger UI. Use the
definition selector in the upper-right corner to switch between Pet, Appointment, and Treatment.

The Gateway retrieves each document from the owning service and replaces its OpenAPI `servers`
entry with the appropriate public prefix. Therefore, Swagger's **Try it out** sends requests to
`/pet`, `/appointment`, or `/treatment` through YARP; it does not call container addresses or
bypass Gateway authentication.

The documentation endpoints themselves are anonymous so Swagger can load them. Proxied API
operations remain protected. Click **Authorize** and paste a current Keycloak access token (the
token value only; Swagger adds `Bearer` automatically).

MCP is listed in the catalog but not represented as an OpenAPI document because `/mcp` uses MCP
JSON-RPC over Streamable HTTP rather than REST. See `MCPServer/README.md` and `MCPServer.http` for
its protocol requests and GitHub Copilot configuration.

## Authentication

Docker containers load OIDC metadata and signing keys from the internal address `http://keycloak:8080/realms/petcare`. Tokens obtained from the host have issuer `http://localhost:8080/realms/petcare`, so Compose configures that value as the accepted issuer. Keycloak realm and client roles are converted to ASP.NET role claims in the Gateway and services.

The Appointment Service's legacy `/auth` endpoints remain available only for local development and existing tests. They are not exposed by the Gateway and are not part of the Docker authentication flow.

## Run and verify

```bash
docker compose up -d --build api-gateway
./scripts/verify-gateway-treatment-mcp.sh
```

The script obtains the imported `vet1` demo token and verifies both real paths:

1. Keycloak -> Gateway -> Treatment Service -> PostgreSQL.
2. Keycloak -> Gateway -> MCP Server -> Treatment Service -> PostgreSQL.

The same requests can be run individually from `ApiGateway.http`.

## Automated tests

```bash
dotnet test tests/ApiGateway.IntegrationTests/ApiGateway.IntegrationTests.csproj
```

The suite starts the real Gateway pipeline and four loopback HTTP services. Its 37 tests cover anonymous and invalid-token rejection, issuer/audience/signature/expiry validation, non-forwarding of rejected requests, every configured cluster and path transform, query/body/correlation/bearer-header forwarding, downstream error propagation, unavailable destinations, MCP Streamable HTTP headers and response content, Keycloak realm/client role conversion, the unified Swagger UI, all rewritten OpenAPI documents, unknown documents, and downstream documentation failures.
