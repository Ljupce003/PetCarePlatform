using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Commands;
using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Entities;
using Moq;
using Xunit;

namespace AppointmentService.Application.Tests.Commands;

public sealed class CreateAvailabilitySlotHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_CreatesAndReturnsSlot()
    {
        var clinicId = Guid.NewGuid();
        var veterinarian = new Veterinarian(clinicId, "Dr. Ana Petrova", "General Practice", "VET-001");
        var start = DateTimeOffset.UtcNow.AddDays(7);
        var end = start.AddMinutes(30);

        var veterinarians = new Mock<IVeterinarianRepository>();
        veterinarians.Setup(repo => repo.GetByIdAsync(veterinarian.VeterinarianId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(veterinarian);

        var slots = new Mock<IAvailabilitySlotRepository>();
        AvailabilitySlot? addedSlot = null;
        slots.Setup(repo => repo.AddAsync(It.IsAny<AvailabilitySlot>(), It.IsAny<CancellationToken>()))
            .Callback<AvailabilitySlot, CancellationToken>((slot, _) => addedSlot = slot)
            .Returns(Task.CompletedTask);
        slots.Setup(repo => repo.SearchAvailableAsync(veterinarian.VeterinarianId, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            [
                new AvailableSlotSearchResult(
                    addedSlot!.AvailabilitySlotId, veterinarian.VeterinarianId, veterinarian.FullName,
                    veterinarian.Specialization, clinicId, "Central Vet Clinic", start, end)
            ]);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new CreateAvailabilitySlotHandler(veterinarians.Object, slots.Object, unitOfWork.Object);
        var result = await handler.HandleAsync(
            new CreateAvailabilitySlotCommand(veterinarian.VeterinarianId, start, end), CancellationToken.None);

        Assert.Equal(veterinarian.VeterinarianId, result.VeterinarianId);
        Assert.Equal("Central Vet Clinic", result.ClinicName);
        Assert.Equal(start, result.StartsAtUtc);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownVeterinarian_ThrowsKeyNotFoundException()
    {
        var veterinarians = new Mock<IVeterinarianRepository>();
        var slots = new Mock<IAvailabilitySlotRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateAvailabilitySlotHandler(veterinarians.Object, slots.Object, unitOfWork.Object);

        var command = new CreateAvailabilitySlotCommand(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.HandleAsync(command, CancellationToken.None));
        slots.Verify(repo => repo.AddAsync(It.IsAny<AvailabilitySlot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithPastStart_ThrowsValidationException()
    {
        var veterinarians = new Mock<IVeterinarianRepository>();
        var slots = new Mock<IAvailabilitySlotRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateAvailabilitySlotHandler(veterinarians.Object, slots.Object, unitOfWork.Object);

        var pastStart = DateTimeOffset.UtcNow.AddDays(-1);
        var command = new CreateAvailabilitySlotCommand(Guid.NewGuid(), pastStart, pastStart.AddMinutes(30));

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, CancellationToken.None));
        veterinarians.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
