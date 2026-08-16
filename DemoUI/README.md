# PetCare Streamlit Demo UI

The demo UI is a lightweight presentation client for the complete PetCare stack. It does not
access a database directly and does not replace any microservice API.

Start the platform and UI:

```bash
docker compose up --build
```

Open [http://localhost:8501](http://localhost:8501).

The UI demonstrates:

- real Keycloak login for the owner, veterinarian, and administrator demo users;
- live health checks and the Pet/Appointment registrations in Consul;
- Pet data and open Appointment slots through the YARP Gateway;
- Appointment scheduling with real Appointment → Consul → Pet ownership verification;
- invalid ownership rejection;
- Appointment Kafka event consumption and notification creation in Treatment Service;
- MCP initialization, tool discovery, and tool invocation over Streamable HTTP.

The MCP playground forwards the selected user's Keycloak bearer token. The MCP server then
forwards that token to the owning service, which remains responsible for role authorization.
