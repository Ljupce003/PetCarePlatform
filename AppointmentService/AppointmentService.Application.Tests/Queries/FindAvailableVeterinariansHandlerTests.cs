using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Queries;
using AppointmentService.Domain.Entities;
using Moq;
using Xunit;

namespace AppointmentService.Application.Tests.Queries;

/// <summary>
/// Covers the composite read behind the MCP "who's free" tool (AppointmentService.Api/Mcp/AppointmentTools.cs) --
/// see FindAvailableVeterinarians.cs for why this composes IClinicRepository + IAvailabilitySlotRepository
/// instead of adding a new repository query.
/// </summary>
public sealed class FindAvailableVeterinariansHandlerTests
{
    private static readonly Guid SkopjeClinicId = Guid.NewGuid();
    private static readonly Guid BitolaClinicId = Guid.NewGuid();
    private static readonly Guid VetAnaId = Guid.NewGuid();
    private static readonly Guid VetMarkoId = Guid.NewGuid();
    private static readonly DateOnly RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

    [Fact]
    public async Task WithNoFilters_GroupsOpenSlotsByVeterinarian()
    {
        var clinics = new Mock<IClinicRepository>();
        var slots = new Mock<IAvailabilitySlotRepository>();
        slots.Setup(repo => repo.SearchAvailableAsync(null, RequestedDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                SlotResult(VetAnaId, "Dr. Ana Petrova", "General Practice", SkopjeClinicId, "Central Vet", hour: 9),
                SlotResult(VetAnaId, "Dr. Ana Petrova", "General Practice", SkopjeClinicId, "Central Vet", hour: 10),
                SlotResult(VetMarkoId, "Dr. Marko Iliev", "Surgery", BitolaClinicId, "Bitola Animal Clinic", hour: 11)
            ]);

        var handler = new FindAvailableVeterinariansHandler(clinics.Object, slots.Object);
        var result = await handler.HandleAsync(new FindAvailableVeterinariansQuery(RequestedDate, null, null), CancellationToken.None);

        Assert.Equal(2, result.Count);
        var ana = Assert.Single(result, v => v.VeterinarianId == VetAnaId);
        Assert.Equal(2, ana.AvailableSlots.Count);
        var marko = Assert.Single(result, v => v.VeterinarianId == VetMarkoId);
        Assert.Single(marko.AvailableSlots);

        clinics.Verify(repo => repo.SearchAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithLocationFilter_OnlyReturnsVeterinariansAtMatchingClinics()
    {
        var clinics = new Mock<IClinicRepository>();
        clinics.Setup(repo => repo.SearchAsync("Skopje", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Clinic.Seed(SkopjeClinicId, "Central Vet", "Skopje", "Bul. Ilinden 1")]);

        var slots = new Mock<IAvailabilitySlotRepository>();
        slots.Setup(repo => repo.SearchAvailableAsync(null, RequestedDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                SlotResult(VetAnaId, "Dr. Ana Petrova", "General Practice", SkopjeClinicId, "Central Vet", hour: 9),
                SlotResult(VetMarkoId, "Dr. Marko Iliev", "Surgery", BitolaClinicId, "Bitola Animal Clinic", hour: 11)
            ]);

        var handler = new FindAvailableVeterinariansHandler(clinics.Object, slots.Object);
        var result = await handler.HandleAsync(new FindAvailableVeterinariansQuery(RequestedDate, "Skopje", null), CancellationToken.None);

        var veterinarian = Assert.Single(result);
        Assert.Equal(VetAnaId, veterinarian.VeterinarianId);
    }

    [Fact]
    public async Task WithSpecializationFilter_OnlyReturnsMatchingVeterinarians()
    {
        var clinics = new Mock<IClinicRepository>();
        var slots = new Mock<IAvailabilitySlotRepository>();
        slots.Setup(repo => repo.SearchAvailableAsync(null, RequestedDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                SlotResult(VetAnaId, "Dr. Ana Petrova", "General Practice", SkopjeClinicId, "Central Vet", hour: 9),
                SlotResult(VetMarkoId, "Dr. Marko Iliev", "Surgery", BitolaClinicId, "Bitola Animal Clinic", hour: 11)
            ]);

        var handler = new FindAvailableVeterinariansHandler(clinics.Object, slots.Object);
        var result = await handler.HandleAsync(new FindAvailableVeterinariansQuery(RequestedDate, null, "surgery"), CancellationToken.None);

        var veterinarian = Assert.Single(result);
        Assert.Equal(VetMarkoId, veterinarian.VeterinarianId);
    }

    [Fact]
    public async Task WithLocationMatchingNoClinics_ReturnsEmptyWithoutQueryingSlots()
    {
        var clinics = new Mock<IClinicRepository>();
        clinics.Setup(repo => repo.SearchAsync("Nowhere", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var slots = new Mock<IAvailabilitySlotRepository>();

        var handler = new FindAvailableVeterinariansHandler(clinics.Object, slots.Object);
        var result = await handler.HandleAsync(new FindAvailableVeterinariansQuery(RequestedDate, "Nowhere", null), CancellationToken.None);

        Assert.Empty(result);
        slots.Verify(repo => repo.SearchAvailableAsync(It.IsAny<Guid?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AvailableSlotSearchResult SlotResult(
        Guid veterinarianId, string veterinarianName, string specialization, Guid clinicId, string clinicName, int hour)
    {
        var start = new DateTimeOffset(RequestedDate.ToDateTime(new TimeOnly(hour, 0), DateTimeKind.Utc));
        return new AvailableSlotSearchResult(
            Guid.NewGuid(), veterinarianId, veterinarianName, specialization, clinicId, clinicName,
            start, start.AddMinutes(30));
    }
}
