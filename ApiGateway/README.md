# PetCare API Gateway

The Gateway is the public HTTP entry point for the PetCare services. YARP validates a Keycloak JWT before forwarding the original bearer token to the selected service.

| Public path | Destination |
| --- | --- |
| `/pet/{**path}` | Pet Service, with `/pet` removed |
| `/appointment/{**path}` | Appointment Service, with `/appointment` removed |
| `/treatment/{**path}` | Treatment & Notification Service, with `/treatment` removed |
| `/mcp` | Shared MCP Server, with the MCP path preserved |

`/health` is intentionally anonymous. Every proxy route uses the default authorization policy.

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

The suite starts the real Gateway pipeline and four loopback HTTP services. It covers anonymous and invalid-token rejection, issuer/audience/signature/expiry validation, non-forwarding of rejected requests, every configured cluster and path transform, query/body/correlation/bearer-header forwarding, downstream error propagation, unavailable destinations, MCP Streamable HTTP headers and response content, and Keycloak realm/client role conversion.
