using System.Net;
using System.Text;
using System.Text.Json;
using PactNet;
using Xunit;

namespace TreatmentAndNotificationService.PactTests;

/// <summary>
/// Consumer-side API contracts for Treatment &amp; Notification Service. Running this suite writes
/// <c>tests/Contracts/pacts/Treatment API Consumer-Treatment &amp; Notification Service.json</c>; a provider
/// verification suite can use that artifact once a database-backed Treatment API test host is added.
/// </summary>
public sealed class TreatmentApiConsumerPactTests
{
    private static readonly Guid PetId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid VeterinarianId = Guid.Parse("b1111111-1111-1111-1111-111111111111");
    private readonly IPactBuilderV4 _pactBuilder;

    public TreatmentApiConsumerPactTests()
    {
        var pact = Pact.V4("Treatment API Consumer", "Treatment & Notification Service", new PactConfig
        {
            PactDir = FindRepositoryRoot().Combine("tests").Combine("Contracts").Combine("pacts").FullName
        });
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetMedicalHistory_ReturnsOrderedExaminations()
    {
        _pactBuilder.UponReceiving("a request for a pet medical history")
            .WithRequest(HttpMethod.Get, $"/api/treatments/pet/{PetId}")
            .WillRespond().WithStatus(HttpStatusCode.OK).WithJsonBody(new[]
            {
                new { Id = Guid.Parse("c1111111-1111-1111-1111-111111111111"), PetId, OwnerId, VeterinarianId,
                    AppointmentId = (Guid?)null, ExaminedAtUtc = "2026-08-01T10:00:00+00:00", Diagnosis = "Otitis",
                    TreatmentPlan = "Ear drops twice daily", Medications = new[] { "Otomax" }, NextControlAtUtc = "2026-08-08T10:00:00+00:00", Notes = "Review in one week" }
            });

        await VerifyAsync(client => client.GetAsync($"/api/treatments/pet/{PetId}"), HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordMedicalExamination_ReturnsCreatedExamination()
    {
        var body = ExaminationRequestJson();
        _pactBuilder.UponReceiving("a valid medical examination")
            .WithRequest(HttpMethod.Post, "/api/treatments").WithHeader("Content-Type", "application/json")
            .WithJsonBody(PactJson(body)).WillRespond().WithStatus(HttpStatusCode.Created)
            .WithHeader("Location", $"/api/treatments/pet/{PetId}")
            .WithJsonBody(new { Id = Guid.Parse("c1111111-1111-1111-1111-111111111111"), PetId, OwnerId, VeterinarianId,
                AppointmentId = (Guid?)null, ExaminedAtUtc = "2026-08-06T10:00:00+00:00", Diagnosis = "Otitis",
                TreatmentPlan = "Ear drops twice daily", Medications = new[] { "Otomax" }, NextControlAtUtc = "2026-08-13T10:00:00+00:00", Notes = "Review in one week" });

        await VerifyAsync(client => client.PostAsync("/api/treatments", Json(body)), HttpStatusCode.Created);
    }

    [Fact]
    public async Task RecordMedicalExamination_WithInvalidFollowUp_ReturnsBadRequest()
    {
        var body = ExaminationRequestJson("2026-08-06T10:00:00+00:00", "2026-08-05T10:00:00+00:00");
        _pactBuilder.UponReceiving("an examination whose follow-up precedes the examination")
            .WithRequest(HttpMethod.Post, "/api/treatments").WithHeader("Content-Type", "application/json")
            .WithJsonBody(PactJson(body)).WillRespond().WithStatus(HttpStatusCode.BadRequest)
            .WithJsonBody(new { status = 400, title = "The request violates a treatment domain rule.", detail = "Follow-up must be after the examination." });

        await VerifyAsync(client => client.PostAsync("/api/treatments", Json(body)), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetVaccinationHistory_ReturnsVaccinations()
    {
        _pactBuilder.UponReceiving("a request for a pet vaccination history")
            .WithRequest(HttpMethod.Get, $"/api/vaccinations/pet/{PetId}")
            .WillRespond().WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new[] { new { Id = Guid.Parse("d1111111-1111-1111-1111-111111111111"), PetId, OwnerId, VeterinarianId,
                VaccineName = "Rabies", AdministeredOn = "2026-02-01", NextDueOn = "2027-02-01", BatchNumber = "RAB-001" } });

        await VerifyAsync(client => client.GetAsync($"/api/vaccinations/pet/{PetId}"), HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNextVaccination_WhenDueVaccinationExists_ReturnsIt()
    {
        _pactBuilder.UponReceiving("a request for an upcoming vaccination")
            .WithRequest(HttpMethod.Get, $"/api/vaccinations/pet/{PetId}/next")
            .WillRespond().WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new { Id = Guid.Parse("d1111111-1111-1111-1111-111111111111"), PetId, OwnerId, VeterinarianId,
                VaccineName = "Rabies", AdministeredOn = "2026-02-01", NextDueOn = "2027-02-01", BatchNumber = "RAB-001" });

        await VerifyAsync(client => client.GetAsync($"/api/vaccinations/pet/{PetId}/next"), HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNextVaccination_WhenNoUpcomingVaccinationExists_ReturnsNotFound()
    {
        _pactBuilder.UponReceiving("a request for a pet without an upcoming vaccination")
            .WithRequest(HttpMethod.Get, $"/api/vaccinations/pet/{PetId}/next")
            .WillRespond().WithStatus(HttpStatusCode.NotFound);

        await VerifyAsync(client => client.GetAsync($"/api/vaccinations/pet/{PetId}/next"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecordVaccination_ReturnsCreatedVaccination()
    {
        var body = VaccinationRequestJson();
        _pactBuilder.UponReceiving("a valid vaccination record")
            .WithRequest(HttpMethod.Post, "/api/vaccinations").WithHeader("Content-Type", "application/json")
            .WithJsonBody(PactJson(body)).WillRespond().WithStatus(HttpStatusCode.Created)
            .WithHeader("Location", $"/api/vaccinations/pet/{PetId}")
            .WithJsonBody(new { Id = Guid.Parse("d1111111-1111-1111-1111-111111111111"), PetId, OwnerId, VeterinarianId,
                VaccineName = "Rabies", AdministeredOn = "2026-08-01", NextDueOn = "2027-08-01", BatchNumber = "RAB-001" });

        await VerifyAsync(client => client.PostAsync("/api/vaccinations", Json(body)), HttpStatusCode.Created);
    }

    [Fact]
    public async Task RecordVaccination_WithInvalidDueDate_ReturnsBadRequest()
    {
        var body = VaccinationRequestJson("2026-08-01", "2026-08-01");
        _pactBuilder.UponReceiving("a vaccination whose due date is not after administration")
            .WithRequest(HttpMethod.Post, "/api/vaccinations").WithHeader("Content-Type", "application/json")
            .WithJsonBody(PactJson(body)).WillRespond().WithStatus(HttpStatusCode.BadRequest)
            .WithJsonBody(new { status = 400, title = "The request violates a treatment domain rule.", detail = "Next vaccine date must be after the administration date." });

        await VerifyAsync(client => client.PostAsync("/api/vaccinations", Json(body)), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOwnerNotifications_ReturnsNotificationHistory()
    {
        _pactBuilder.UponReceiving("a request for an owner notification history")
            .WithRequest(HttpMethod.Get, $"/api/notifications/owner/{OwnerId}")
            .WillRespond().WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new[] { new { Id = Guid.Parse("e1111111-1111-1111-1111-111111111111"), OwnerId, PetId,
                Type = "FollowUpReminder", Title = "Veterinary follow-up", Message = "Follow-up is due.",
                ScheduledForUtc = "2026-08-12T10:00:00+00:00", Status = "Pending", CreatedAtUtc = "2026-08-06T10:00:00+00:00", SentAtUtc = (string?)null } });

        await VerifyAsync(client => client.GetAsync($"/api/notifications/owner/{OwnerId}"), HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateNotification_ReturnsCreatedNotification()
    {
        var body = NotificationRequestJson();
        _pactBuilder.UponReceiving("a valid notification")
            .WithRequest(HttpMethod.Post, "/api/notifications").WithHeader("Content-Type", "application/json")
            .WithJsonBody(PactJson(body)).WillRespond().WithStatus(HttpStatusCode.Created)
            .WithHeader("Location", "/api/notifications/e1111111-1111-1111-1111-111111111111")
            .WithJsonBody(new { Id = Guid.Parse("e1111111-1111-1111-1111-111111111111"), OwnerId, PetId,
                Type = "FollowUpReminder", Title = "Veterinary follow-up", Message = "Follow-up is due.",
                ScheduledForUtc = "2026-08-12T10:00:00+00:00", Status = "Pending", CreatedAtUtc = "2026-08-06T10:00:00+00:00", SentAtUtc = (string?)null });

        await VerifyAsync(client => client.PostAsync("/api/notifications", Json(body)), HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateNotification_WithMissingSourceEventId_ReturnsBadRequest()
    {
        var body = "{\"ownerId\":\"11111111-1111-1111-1111-111111111111\",\"petId\":\"a1111111-1111-1111-1111-111111111111\",\"type\":\"FollowUpReminder\",\"title\":\"Veterinary follow-up\",\"message\":\"Follow-up is due.\",\"scheduledForUtc\":\"2026-08-12T10:00:00+00:00\",\"sourceEventId\":null}";
        _pactBuilder.UponReceiving("a notification without a source event id")
            .WithRequest(HttpMethod.Post, "/api/notifications").WithHeader("Content-Type", "application/json")
            .WithJsonBody(PactJson(body)).WillRespond().WithStatus(HttpStatusCode.BadRequest)
            .WithJsonBody(new { status = 400, title = "The request violates a treatment domain rule.", detail = "Notification source event id is required." });

        await VerifyAsync(client => client.PostAsync("/api/notifications", Json(body)), HttpStatusCode.BadRequest);
    }

    private async Task VerifyAsync(Func<HttpClient, Task<HttpResponseMessage>> request, HttpStatusCode expectedStatus)
    {
        await _pactBuilder.VerifyAsync(async context =>
        {
            using var client = new HttpClient { BaseAddress = context.MockServerUri };
            using var response = await request(client);
            Assert.Equal(expectedStatus, response.StatusCode);
        });
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static JsonElement PactJson(string body) => JsonDocument.Parse(body).RootElement.Clone();

    private static string ExaminationRequestJson(string examinedAtUtc = "2026-08-06T10:00:00+00:00", string nextControlAtUtc = "2026-08-13T10:00:00+00:00") =>
        $"{{\"petId\":\"{PetId}\",\"ownerId\":\"{OwnerId}\",\"veterinarianId\":\"{VeterinarianId}\",\"appointmentId\":null,\"examinedAtUtc\":\"{examinedAtUtc}\",\"diagnosis\":\"Otitis\",\"treatmentPlan\":\"Ear drops twice daily\",\"medications\":[\"Otomax\"],\"nextControlAtUtc\":\"{nextControlAtUtc}\",\"notes\":\"Review in one week\"}}";

    private static string VaccinationRequestJson(string administeredOn = "2026-08-01", string nextDueOn = "2027-08-01") =>
        $"{{\"petId\":\"{PetId}\",\"ownerId\":\"{OwnerId}\",\"veterinarianId\":\"{VeterinarianId}\",\"vaccineName\":\"Rabies\",\"administeredOn\":\"{administeredOn}\",\"nextDueOn\":\"{nextDueOn}\",\"batchNumber\":\"RAB-001\"}}";

    private static string NotificationRequestJson() =>
        $"{{\"ownerId\":\"{OwnerId}\",\"petId\":\"{PetId}\",\"type\":\"FollowUpReminder\",\"title\":\"Veterinary follow-up\",\"message\":\"Follow-up is due.\",\"scheduledForUtc\":\"2026-08-12T10:00:00+00:00\",\"sourceEventId\":\"manual:follow-up:1\"}}";

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetCarePlatform.slnx")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Repository root (PetCarePlatform.slnx) was not found.");
    }
}

internal static class DirectoryInfoExtensions
{
    public static DirectoryInfo Combine(this DirectoryInfo directory, string child) => new(Path.Combine(directory.FullName, child));
}
