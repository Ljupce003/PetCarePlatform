using System.Net;
using System.Net.Http.Json;
using AppointmentService.Application.Dtos;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.Persistence;
using Shared.AppointmentEvents;
using Shared.Messaging;
using Xunit;

namespace AppointmentService.Api.IntegrationTests;

/// <summary>
/// Exercises the full REST + domain + event-publishing path together: each test gets its own
/// freshly-seeded InMemory database (see IAsyncLifetime below), so booking/rescheduling/
/// cancelling here can never collide with another test's slots.
/// </summary>
public sealed class AppointmentWorkflowTests : IAsyncLifetime
{
    private AppointmentServiceApiFactory _factory = null!;
    private HttpClient _ownerClient = null!;

    public async Task InitializeAsync()
    {
        _factory = new AppointmentServiceApiFactory();
        _ownerClient = await _factory.CreateAuthenticatedClientAsync("owner1", "Owner123!");
    }

    public async Task DisposeAsync()
    {
        _ownerClient.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task FullLifecycle_ScheduleRescheduleThenCancel_UpdatesStateAndPublishesEachEvent()
    {
        var openSlots = await _ownerClient.GetFromJsonAsync<List<AvailableSlotDto>>(
            $"/slots?veterinarianId={AppointmentDbInitializer.DemoVeterinarianId}", JsonDefaults.CaseInsensitive);
        Assert.NotNull(openSlots);
        Assert.NotEmpty(openSlots); // seed leaves 5 of the vet's 6 slots open; slot 0 is pre-booked
        var firstSlot = openSlots[0];

        // 1. Schedule
        var scheduleResponse = await _ownerClient.PostAsJsonAsync("/appointments", new
        {
            petId = AppointmentDbInitializer.DemoPetId,
            ownerId = AppointmentDbInitializer.DemoOwnerId,
            availabilitySlotId = firstSlot.AvailabilitySlotId,
            reason = "Routine vaccination"
        });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);
        var booked = await scheduleResponse.Content.ReadFromJsonAsync<AppointmentDto>(JsonDefaults.CaseInsensitive);
        Assert.NotNull(booked);
        Assert.Equal(AppointmentStatus.Scheduled, booked!.Status);

        var scheduledEvent = Assert.Single(_factory.Events.Published, e => e.Message is AppointmentScheduledEvent);
        Assert.Equal(PetCareTopics.Appointments, scheduledEvent.Topic);
        Assert.Equal(booked.AppointmentId, ((AppointmentScheduledEvent)scheduledEvent.Message).AppointmentId);

        // 2. Shows up in "upcoming" for the same owner
        var upcoming = await _ownerClient.GetFromJsonAsync<List<AppointmentDto>>(
            $"/appointments/upcoming?ownerId={AppointmentDbInitializer.DemoOwnerId}", JsonDefaults.CaseInsensitive);
        Assert.Contains(upcoming!, a => a.AppointmentId == booked.AppointmentId);

        // 3. Reschedule onto a different open slot (the one just booked no longer shows up as open)
        var remainingSlots = await _ownerClient.GetFromJsonAsync<List<AvailableSlotDto>>(
            $"/slots?veterinarianId={AppointmentDbInitializer.DemoVeterinarianId}", JsonDefaults.CaseInsensitive);
        Assert.NotNull(remainingSlots);
        Assert.NotEmpty(remainingSlots);
        var newSlot = remainingSlots[0];

        var rescheduleResponse = await _ownerClient.PutAsJsonAsync(
            $"/appointments/{booked.AppointmentId}/reschedule",
            new { newAvailabilitySlotId = newSlot.AvailabilitySlotId });
        Assert.Equal(HttpStatusCode.OK, rescheduleResponse.StatusCode);
        var rescheduled = await rescheduleResponse.Content.ReadFromJsonAsync<AppointmentDto>(JsonDefaults.CaseInsensitive);
        Assert.Equal(newSlot.AvailabilitySlotId, rescheduled!.AvailabilitySlotId);
        Assert.Contains(_factory.Events.Published, e => e.Message is AppointmentRescheduledEvent);

        // 4. Cancel
        var cancelResponse = await _ownerClient.DeleteAsync(
            $"/appointments/{booked.AppointmentId}?reason=Changed%20my%20mind");
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<AppointmentDto>(JsonDefaults.CaseInsensitive);
        Assert.Equal(AppointmentStatus.Cancelled, cancelled!.Status);
        Assert.Contains(_factory.Events.Published, e => e.Message is AppointmentCancelledEvent);

        // No longer upcoming
        var upcomingAfterCancel = await _ownerClient.GetFromJsonAsync<List<AppointmentDto>>(
            $"/appointments/upcoming?ownerId={AppointmentDbInitializer.DemoOwnerId}", JsonDefaults.CaseInsensitive);
        Assert.DoesNotContain(upcomingAfterCancel!, a => a.AppointmentId == booked.AppointmentId);
    }

    [Fact]
    public async Task Schedule_WithoutAuthorizationHeader_Returns401()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync("/appointments", new
        {
            petId = AppointmentDbInitializer.DemoPetId,
            ownerId = AppointmentDbInitializer.DemoOwnerId,
            availabilitySlotId = Guid.NewGuid(),
            reason = "Routine vaccination"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_AsVeterinarian_Returns403Forbidden()
    {
        var vetClient = await _factory.CreateAuthenticatedClientAsync("vet1", "Vet123!");

        var response = await vetClient.PostAsJsonAsync("/appointments", new
        {
            petId = AppointmentDbInitializer.DemoPetId,
            ownerId = AppointmentDbInitializer.DemoOwnerId,
            availabilitySlotId = Guid.NewGuid(),
            reason = "Routine vaccination"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_WithUnknownAvailabilitySlot_Returns404NotFound()
    {
        var response = await _ownerClient.PostAsJsonAsync("/appointments", new
        {
            petId = AppointmentDbInitializer.DemoPetId,
            ownerId = AppointmentDbInitializer.DemoOwnerId,
            availabilitySlotId = Guid.NewGuid(),
            reason = "Routine vaccination"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_WithEmptyReason_Returns400BadRequest()
    {
        var openSlots = await _ownerClient.GetFromJsonAsync<List<AvailableSlotDto>>(
            $"/slots?veterinarianId={AppointmentDbInitializer.DemoVeterinarianId}", JsonDefaults.CaseInsensitive);

        var response = await _ownerClient.PostAsJsonAsync("/appointments", new
        {
            petId = AppointmentDbInitializer.DemoPetId,
            ownerId = AppointmentDbInitializer.DemoOwnerId,
            availabilitySlotId = openSlots![0].AvailabilitySlotId,
            reason = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
