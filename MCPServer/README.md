# PetCare MCP Server

The shared MCP server exposes PetCare operations to MCP clients without accessing service databases. Each tool delegates to the owning microservice through its HTTP API, preserving the platform's service boundaries.

## Current endpoint

- Local development: `http://localhost:5044/mcp`
- Docker Compose: `http://localhost:7001/mcp`
- Health: `/health` (anonymous)
- Transport: stateless Streamable HTTP
- Authentication: bearer JWT using the same temporary issuer, audience, and development key as Appointment and Treatment services

The `/mcp` endpoint requires a valid token. The same bearer token is forwarded to downstream services, which perform their own authorization checks. Treatment write tools therefore require the Treatment Service's `veterinarian` or `admin` role.

## Registered Pet tools

- `get_pet`
- `get_owner_pets`

## Registered Appointment tools

- `find_available_veterinarians`
- `get_upcoming_appointments`

## Registered Treatment tools

- `get_medical_history`
- `get_vaccination_history`
- `get_next_vaccination`
- `record_medical_examination`
- `record_vaccination`

`WithToolsFromAssembly()` discovers classes marked with `[McpServerToolType]` and methods marked with `[McpServerTool]`. All current Pet, Appointment, and Treatment contributions are registered this way.

## Run locally

Start Pet Service on `5101`, Appointment Service on `5102`, and Treatment Service on `5103`, then run:

```bash
dotnet run --project MCPServer/MCPServer.csproj --launch-profile http
```

Use `MCPServer.http` to obtain a demo token, initialize MCP, and list tools.

## Run with Docker

```bash
docker compose up --build mcp-server
```

Inside Docker, the MCP server uses the Compose service names on port `8080`; local development uses ports `5101` through `5103`.
The public Docker endpoint is routed through the API Gateway at `http://localhost:7000/mcp`.

## Tests

```bash
dotnet test tests/MCPServer.IntegrationTests/MCPServer.IntegrationTests.csproj
```

The integration suite verifies anonymous health access, MCP authentication, initialization, all nine tools, downstream route selection, response parsing, and bearer-token forwarding. Its required end-to-end test starts a real Treatment API with PostgreSQL, records an examination through MCP, and reads it back through MCP.

## Gateway end-to-end verification

Run `scripts/verify-gateway-treatment-mcp.sh` after starting the Gateway stack. It obtains a real Keycloak veterinarian token, records and reads Treatment data through YARP, and reads the same persisted data through the MCP route.
