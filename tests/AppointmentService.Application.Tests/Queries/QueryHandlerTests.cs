using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Queries;
using AppointmentService.Domain.Entities;
using Moq;
using Xunit;

namespace AppointmentService.Application.Tests.Queries;

public sealed class QueryHandlerTests
{
    [Fact]
    public async Task SearchClinics_PassesLocationThroughAndMapsResultsToDtos()
    {
        var clinics = new Mock<IClinicRepository>();
        var clinic = Clinic.Seed(Guid.NewGuid(), "Central Vet Clinic", "Skopje", "Bul. Ilinden 1");
        clinics.Setup(repo => repo.SearchAsync("Skopje", It.IsAny<CancellationToken>())).ReturnsAsync([clinic]);

        var handler = new SearchClinicsHandler(clinics.Object);
        var result = await handler.HandleAsync(new SearchClinicsQuery("Skopje"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(clinic.ClinicId, result[0].ClinicId);
        Assert.Equal("Central Vet Clinic", result[0].Name);
    }

    [Fact]
    public async Task SearchVeterinarians_PassesFiltersThroughAndMapsResultsToDtos()
    {
        var veterinarians = new Mock<IVeterinarianRepository>();
        var clinicId = Guid.NewGuid();
        var veterinarian = new Veterinarian(clinicId, "Dr. Ana Petrova", "Surgery", "VET-001");
        veterinarians.Setup(repo => repo.SearchAsync(clinicId, "Surgery", It.IsAny<CancellationToken>())).ReturnsAsync([veterinarian]);

        var handler = new SearchVeterinariansHandler(veterinarians.Object);
        var result = await handler.HandleAsync(new SearchVeterinariansQuery(clinicId, "Surgery"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(veterinarian.VeterinarianId, result[0].VeterinarianId);
        Assert.Equal("Surgery", result[0].Specialization);
    }

    [Fact]
    public async Task SearchAvailableSlots_WithPastDate_ThrowsValidationException()
    {
        var slots = new Mock<IAvailabilitySlotRepository>();
        var handler = new SearchAvailableSlotsHandler(slots.Object);
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(new SearchAvailableSlotsQuery(null, pastDate), CancellationToken.None));

        slots.Verify(repo => repo.SearchAvailableAsync(It.IsAny<Guid?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAvailableSlots_WithFutureDate_ReturnsMappedResults()
    {
        var slots = new Mock<IAvailabilitySlotRepository>();
        var veterinarianId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var searchResult = new AvailableSlotSearchResult(
            Guid.NewGuid(), veterinarianId, "Dr. Ana Petrova", "General Practice",
            Guid.NewGuid(), "Central Vet Clinic", start, start.AddMinutes(30));
        slots.Setup(repo => repo.SearchAvailableAsync(veterinarianId, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([searchResult]);

        var handler = new SearchAvailableSlotsHandler(slots.Object);
        var result = await handler.HandleAsync(new SearchAvailableSlotsQuery(veterinarianId, null), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(searchResult.AvailabilitySlotId, result[0].AvailabilitySlotId);
    }

    [Fact]
    public async Task GetUpcomingAppointments_WithEmptyOwnerId_ThrowsValidationException()
    {
        var appointments = new Mock<IAppointmentRepository>();
        var handler = new GetUpcomingAppointmentsHandler(appointments.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(new GetUpcomingAppointmentsQuery(Guid.Empty), CancellationToken.None));

        appointments.Verify(repo => repo.GetUpcomingByOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUpcomingAppointments_WithValidOwnerId_ReturnsMappedResults()
    {
        var appointments = new Mock<IAppointmentRepository>();
        var ownerId = Guid.NewGuid();
        var appointment = new Appointment(
            Guid.NewGuid(), ownerId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30), "Annual check-up");
        appointments.Setup(repo => repo.GetUpcomingByOwnerAsync(ownerId, It.IsAny<CancellationToken>())).ReturnsAsync([appointment]);

        var handler = new GetUpcomingAppointmentsHandler(appointments.Object);
        var result = await handler.HandleAsync(new GetUpcomingAppointmentsQuery(ownerId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(appointment.AppointmentId, result[0].AppointmentId);
    }
}
