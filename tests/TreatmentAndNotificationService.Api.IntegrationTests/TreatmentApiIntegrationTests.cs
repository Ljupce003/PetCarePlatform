using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Infrastructure.Persistence;
using Xunit;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

/// <summary>
/// End-to-end HTTP tests: requests cross the real MVC/controller/application/domain/repository
/// pipeline and assertions inspect data written through Npgsql to a real PostgreSQL container.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class TreatmentApiIntegrationTests : IAsyncLifetime
{
    private static readonly Guid PetId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid VeterinarianId = Guid.Parse("b1111111-1111-1111-1111-111111111111");
    private readonly TreatmentApiFactory _factory;
    private HttpClient _client = null!;

    public TreatmentApiIntegrationTests(PostgreSqlFixture database) => _factory = new TreatmentApiFactory(database.ConnectionString);

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(); // starts the real host and applies the production migrations
        await _factory.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task DatabaseConnectivity_AndMigrations_AreHealthy()
    {
        var result = await _factory.WithDbContextAsync(async db => new
        {
            CanConnect = await db.Database.CanConnectAsync(),
            AppliedMigrations = await db.Database.GetAppliedMigrationsAsync()
        });

        Assert.True(result.CanConnect);
        Assert.Contains("20260804125504_addedDomainEntities", result.AppliedMigrations);
        Assert.Contains("20260804150000_AddNotificationFailureReason", result.AppliedMigrations);
    }

    [Fact]
    public async Task RecordExamination_PersistsMedicalRecord_CreatesReminder_AndReturnsItInHistory()
    {
        var followUp = DateTimeOffset.UtcNow.AddDays(10);
        var response = await _client.PostAsJsonAsync("/api/treatments", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, appointmentId = (Guid?)null,
            examinedAtUtc = DateTimeOffset.UtcNow, diagnosis = "Acute otitis", treatmentPlan = "Ear drops twice daily",
            medications = new[] { "Otomax", "otomax", " " }, nextControlAtUtc = followUp, notes = "Review in ten days"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MedicalExaminationDto>(JsonDefaults.CaseInsensitive);
        Assert.NotNull(created);
        Assert.Equal(PetId, created!.PetId);
        Assert.Single(created.Medications); // aggregate normalized duplicate medication input

        var persisted = await _factory.WithDbContextAsync(async db => new
        {
            Examination = await db.MedicalExaminations.SingleAsync(),
            Reminder = await db.Notifications.SingleAsync()
        });
        Assert.Equal(created.Id, persisted.Examination.Id);
        Assert.Equal("Acute otitis", persisted.Examination.Diagnosis.Value);
        Assert.Equal(NotificationType.FollowUpReminder, persisted.Reminder.Type);
        Assert.Equal(NotificationStatus.Pending, persisted.Reminder.Status);
        Assert.Equal($"examination:{created.Id}", persisted.Reminder.SourceEventId.Value);

        var history = await _client.GetFromJsonAsync<List<MedicalExaminationDto>>(
            $"/api/treatments/pet/{PetId}", JsonDefaults.CaseInsensitive);
        Assert.Contains(history!, item => item.Id == created.Id);
    }

    [Fact]
    public async Task RecordExamination_WithFollowUpBeforeExamination_Returns400_AndWritesNothing()
    {
        var response = await _client.PostAsJsonAsync("/api/treatments", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, appointmentId = (Guid?)null,
            examinedAtUtc = "2026-08-06T10:00:00+00:00", diagnosis = "Otitis", treatmentPlan = "Ear drops",
            medications = new[] { "Otomax" }, nextControlAtUtc = "2026-08-05T10:00:00+00:00", notes = "Review"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountAsync(db => db.MedicalExaminations));
        Assert.Equal(0, await CountAsync(db => db.Notifications));
    }

    [Fact]
    public async Task RecordVaccination_PersistsReminder_AndExposesHistoryAndNextDueVaccination()
    {
        var response = await _client.PostAsJsonAsync("/api/vaccinations", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, vaccineName = "Rabies",
            administeredOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), nextDueOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), batchNumber = "RAB-001"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<VaccinationDto>(JsonDefaults.CaseInsensitive);
        Assert.NotNull(created);

        var reminder = await _factory.WithDbContextAsync(db => db.Notifications.SingleAsync());
        Assert.Equal(NotificationType.VaccinationReminder, reminder.Type);
        Assert.Equal($"vaccination:{created!.Id}", reminder.SourceEventId.Value);

        var history = await _client.GetFromJsonAsync<List<VaccinationDto>>(
            $"/api/vaccinations/pet/{PetId}", JsonDefaults.CaseInsensitive);
        Assert.Contains(history!, item => item.Id == created.Id);

        var next = await _client.GetFromJsonAsync<VaccinationDto>(
            $"/api/vaccinations/pet/{PetId}/next", JsonDefaults.CaseInsensitive);
        Assert.Equal(created.Id, next!.Id);
    }

    [Fact]
    public async Task GetNextVaccination_WhenNoFutureDueDateExists_Returns404()
    {
        var response = await _client.GetAsync($"/api/vaccinations/pet/{PetId}/next");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordVaccination_WithNonFutureDueDate_Returns400_AndWritesNothing()
    {
        var response = await _client.PostAsJsonAsync("/api/vaccinations", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, vaccineName = "Rabies",
            administeredOn = "2026-08-01", nextDueOn = "2026-08-01", batchNumber = "RAB-001"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountAsync(db => db.Vaccinations));
        Assert.Equal(0, await CountAsync(db => db.Notifications));
    }

    [Fact]
    public async Task CreateNotification_PersistsIt_ReadsItByOwner_AndRejectsDuplicateSourceEvent()
    {
        const string sourceEventId = "manual:follow-up:1";
        var payload = new
        {
            ownerId = OwnerId, petId = PetId, type = "FollowUpReminder", title = "Veterinary follow-up",
            message = "Follow-up is due.", scheduledForUtc = "2026-08-12T10:00:00+00:00", sourceEventId
        };

        var create = await _client.PostAsJsonAsync("/api/notifications", payload);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var notification = await create.Content.ReadFromJsonAsync<NotificationDto>(JsonDefaults.CaseInsensitive);
        Assert.NotNull(notification);
        Assert.Equal(NotificationStatus.Pending, notification!.Status);

        var history = await _client.GetFromJsonAsync<List<NotificationDto>>(
            $"/api/notifications/owner/{OwnerId}", JsonDefaults.CaseInsensitive);
        Assert.Contains(history!, item => item.Id == notification.Id);

        var duplicate = await _client.PostAsJsonAsync("/api/notifications", payload);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(1, await CountAsync(db => db.Notifications));
    }

    [Fact]
    public async Task CreateNotification_WithoutSourceEventId_Returns400_AndWritesNothing()
    {
        var response = await _client.PostAsJsonAsync("/api/notifications", new
        {
            ownerId = OwnerId, petId = PetId, type = "FollowUpReminder", title = "Veterinary follow-up",
            message = "Follow-up is due.", scheduledForUtc = "2026-08-12T10:00:00+00:00", sourceEventId = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountAsync(db => db.Notifications));
    }

    [Fact]
    public async Task Health_ReturnsOk_FromTheRunningApplication()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EmptyDatabase_ReturnsEmptyHistories_ForEveryReadCollection()
    {
        var examinations = await _client.GetFromJsonAsync<List<MedicalExaminationDto>>(
            $"/api/treatments/pet/{PetId}", JsonDefaults.CaseInsensitive);
        var vaccinations = await _client.GetFromJsonAsync<List<VaccinationDto>>(
            $"/api/vaccinations/pet/{PetId}", JsonDefaults.CaseInsensitive);
        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>(
            $"/api/notifications/owner/{OwnerId}", JsonDefaults.CaseInsensitive);

        Assert.Empty(examinations!);
        Assert.Empty(vaccinations!);
        Assert.Empty(notifications!);
    }

    [Fact]
    public async Task RecordExamination_WithoutFollowUp_DoesNotCreateNotification_AndHistoryIsNewestFirst()
    {
        var older = await RecordExaminationAsync(DateTimeOffset.UtcNow.AddDays(-2), followUp: null);
        var newer = await RecordExaminationAsync(DateTimeOffset.UtcNow.AddDays(-1), followUp: null);

        var notifications = await CountAsync(db => db.Notifications);
        var history = await _client.GetFromJsonAsync<List<MedicalExaminationDto>>(
            $"/api/treatments/pet/{PetId}", JsonDefaults.CaseInsensitive);

        Assert.Equal(0, notifications);
        Assert.Equal([newer.Id, older.Id], history!.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task RecordVaccination_WithoutNextDueDate_DoesNotCreateReminder()
    {
        var response = await _client.PostAsJsonAsync("/api/vaccinations", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, vaccineName = "Rabies",
            administeredOn = "2026-08-01", nextDueOn = (DateOnly?)null, batchNumber = "RAB-001"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, await CountAsync(db => db.Vaccinations));
        Assert.Equal(0, await CountAsync(db => db.Notifications));
    }

    [Fact]
    public async Task ReminderDates_ThatWouldBeInThePast_AreScheduledImmediately()
    {
        var beforeRequest = DateTimeOffset.UtcNow;
        var examination = await RecordExaminationAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(12));
        var vaccinationResponse = await _client.PostAsJsonAsync("/api/vaccinations", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, vaccineName = "DHPP",
            administeredOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), nextDueOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), batchNumber = "DHPP-001"
        });
        Assert.Equal(HttpStatusCode.Created, vaccinationResponse.StatusCode);

        var reminders = await _factory.WithDbContextAsync(db => db.Notifications
            .OrderBy(item => item.Type)
            .ToListAsync());
        Assert.Equal(2, reminders.Count);
        Assert.All(reminders, reminder => Assert.True(reminder.ScheduledForUtc >= beforeRequest));
        Assert.Contains(reminders, item => item.SourceEventId.Value == $"examination:{examination.Id}");
    }

    [Fact]
    public async Task GetNextVaccination_ChoosesClosestFutureDueDate_AndExcludesPastDueVaccinations()
    {
        await RecordVaccinationAsync("Past due", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        var later = await RecordVaccinationAsync("Later", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        var closest = await RecordVaccinationAsync("Closest", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));

        var response = await _client.GetAsync($"/api/vaccinations/pet/{PetId}/next");
        var next = await response.Content.ReadFromJsonAsync<VaccinationDto>(JsonDefaults.CaseInsensitive);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(closest.Id, next!.Id);
        Assert.NotEqual(later.Id, next.Id);
    }

    [Fact]
    public async Task GetOwnerNotifications_ReturnsNewestFirst()
    {
        var old = await CreateNotificationAsync("manual:old", DateTimeOffset.UtcNow.AddDays(1));
        var recent = await CreateNotificationAsync("manual:recent", DateTimeOffset.UtcNow.AddDays(2));

        var history = await _client.GetFromJsonAsync<List<NotificationDto>>(
            $"/api/notifications/owner/{OwnerId}", JsonDefaults.CaseInsensitive);

        Assert.Equal([recent.Id, old.Id], history!.Select(item => item.Id).ToArray());
    }

    [Theory]
    [InlineData("treatments")]
    [InlineData("vaccinations")]
    [InlineData("notifications")]
    public async Task RouteConstraint_WithMalformedGuid_Returns404(string resource)
    {
        var path = resource == "notifications"
            ? $"/api/{resource}/owner/not-a-guid"
            : $"/api/{resource}/pet/not-a-guid";

        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("diagnosis")]
    [InlineData("treatmentPlan")]
    public async Task RecordExamination_WithMissingRequiredClinicalField_Returns400(string missingField)
    {
        var response = await _client.PostAsJsonAsync("/api/treatments", new Dictionary<string, object?>
        {
            ["petId"] = PetId,
            ["ownerId"] = OwnerId,
            ["veterinarianId"] = VeterinarianId,
            ["examinedAtUtc"] = DateTimeOffset.UtcNow,
            ["diagnosis"] = missingField == "diagnosis" ? null : "Otitis",
            ["treatmentPlan"] = missingField == "treatmentPlan" ? null : "Ear drops"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountAsync(db => db.MedicalExaminations));
    }

    [Fact]
    public async Task RecordVaccination_WithMissingVaccineName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/vaccinations", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, vaccineName = (string?)null,
            administeredOn = "2026-08-01", nextDueOn = "2027-08-01", batchNumber = "RAB-001"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountAsync(db => db.Vaccinations));
    }

    private async Task<MedicalExaminationDto> RecordExaminationAsync(DateTimeOffset examinedAt, DateTimeOffset? followUp)
    {
        var response = await _client.PostAsJsonAsync("/api/treatments", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, appointmentId = (Guid?)null,
            examinedAtUtc = examinedAt, diagnosis = "Routine examination", treatmentPlan = "Continue current care",
            medications = Array.Empty<string>(), nextControlAtUtc = followUp, notes = (string?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MedicalExaminationDto>(JsonDefaults.CaseInsensitive))!;
    }

    private async Task<VaccinationDto> RecordVaccinationAsync(string vaccineName, DateOnly administeredOn, DateOnly nextDueOn)
    {
        var response = await _client.PostAsJsonAsync("/api/vaccinations", new
        {
            petId = PetId, ownerId = OwnerId, veterinarianId = VeterinarianId, vaccineName,
            administeredOn, nextDueOn, batchNumber = "TEST-001"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VaccinationDto>(JsonDefaults.CaseInsensitive))!;
    }

    private async Task<NotificationDto> CreateNotificationAsync(string sourceEventId, DateTimeOffset scheduledForUtc)
    {
        var response = await _client.PostAsJsonAsync("/api/notifications", new
        {
            ownerId = OwnerId, petId = PetId, type = "FollowUpReminder", title = sourceEventId,
            message = "Follow-up is due.", scheduledForUtc, sourceEventId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotificationDto>(JsonDefaults.CaseInsensitive))!;
    }

    private Task<int> CountAsync(Func<TreatmentDbContext, IQueryable<object>> set) =>
        _factory.WithDbContextAsync(db => set(db).CountAsync());
}
