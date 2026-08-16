# Member 3 — Presentation and Demo Notes

This is the prepared Member 3 contribution for the final five-minute team video. Keep this section to approximately 75–90 seconds so the other two service owners have enough time.

## Spoken explanation

> I implemented the Treatment and Notification bounded context, the shared MCP server integration, and the API Gateway verification. The Treatment service owns medical examinations, vaccinations, and notifications in its own PostgreSQL database. Its domain layer validates diagnoses, treatment plans, vaccination schedules, and one-way notification status transitions.
>
> Appointment Service publishes scheduled, cancelled, and rescheduled events to Kafka. My hosted consumer uses Kafka long polling, disables automatic commits, and commits an offset only after notification processing succeeds. If the same event is replayed, the unique SourceEventId prevents a duplicate notification. Invalid events are retried and then moved to a dead-letter topic so later events can continue.
>
> A second hosted worker checks due notifications every fifteen seconds. For this course demonstration, delivery is a structured console log. Successful notifications become Sent and individual failures become Failed. E-mail and SMS are deliberately outside our scope because they add external-provider setup but do not demonstrate another required architecture concept.
>
> The YARP Gateway validates Keycloak tokens and routes both REST and MCP requests. The shared MCP server exposes thirteen Pet, Appointment, Treatment, and vaccination tools. It never accesses a service database directly: each tool calls the owning service's REST API and forwards the user's bearer token, so each service still makes the final authorization decision. In the architecture it is the AI-facing adapter over the existing service boundaries. We tested it through the Streamlit MCP playground, the terminal verification script, and automated protocol and end-to-end tests.

## On-screen demonstration

1. Show the architecture diagram and point to Appointment → Kafka → Treatment and Client → Gateway → MCP → Treatment.
2. Open [the Streamlit demo](http://localhost:8501), select **MCP playground**, initialize MCP, list the thirteen tools, and call `get_medical_history` or `get_owner_pets`.
3. Point out the displayed path: Streamlit → Gateway `/mcp` → MCP Server → owning REST service. This shows both how MCP communicates and its role in the architecture.
4. Run `./scripts/verify-gateway-treatment-mcp.sh` and show its three `PASS` lines as terminal-test evidence.
5. Run `./scripts/verify-treatment-notification-worker.sh`, then briefly show `docker compose logs -f treatment-and-notification-service` for its console delivery.
6. If time permits, show that an owner write returns `403` while a veterinarian write succeeds.

## Important implementation points if asked

- `AddHostedService` creates one hosted-worker instance and calls `ExecuteAsync` once; the method's loop performs repeated work until application cancellation.
- `consumer.Consume(stoppingToken)` blocks efficiently in the native Kafka client and uses broker long polling; it is not a busy loop.
- Offsets are committed after successful database work. Replay is expected and safe because notification creation is idempotent.
- Poison records are published to `petcare.appointments.dlq` before the original offset is committed.
- The delivery worker and Kafka consumer are separate because message ingestion and scheduled delivery have different timing and failure behavior.
- Console delivery is the final course adapter, not an unfinished provider integration.

## Verification commands

```bash
dotnet test tests/TreatmentAndNotificationService.Domain.Tests/TreatmentAndNotificationService.Domain.Tests.csproj
dotnet test tests/TreatmentAndNotificationService.Worker.Tests/TreatmentAndNotificationService.Worker.Tests.csproj
dotnet test tests/TreatmentAndNotificationService.Api.IntegrationTests/TreatmentAndNotificationService.Api.IntegrationTests.csproj
dotnet test tests/TreatmentAndNotificationService.PactTests/TreatmentAndNotificationService.PactTests.csproj
dotnet test tests/MCPServer.IntegrationTests/MCPServer.IntegrationTests.csproj
dotnet test tests/ApiGateway.IntegrationTests/ApiGateway.IntegrationTests.csproj
```
