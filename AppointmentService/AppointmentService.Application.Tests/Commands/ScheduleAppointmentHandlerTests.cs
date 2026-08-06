using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Commands;
using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.AppointmentEvents;
using Shared.Messaging;
using Xunit;

namespace AppointmentService.Application.Tests.Commands;

public sealed class ScheduleAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IAvailabilitySlotRepository> _slots = new();
    private readonly Mock<IVeterinarianRepository> _veterinarians = new();
    private readonly Mock<IPetVerificationClient> _petVerification = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IIntegrationEventPublisher> _eventPublisher = new();

    private static readonly Guid PetId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid ClinicId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_WithValidCommand_ReservesSlotAndPublishesScheduledEvent()
    {
        var veterinarian = new Veterinarian(ClinicId, "Dr. Ana Petrova", "General Practice", "VET-001");
        var slot = new AvailabilitySlot(veterinarian.VeterinarianId, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30));
        var command = new ScheduleAppointmentCommand(PetId, OwnerId, slot.AvailabilitySlotId, "Annual check-up");

        _petVerification.Setup(client => client.VerifyAsync(PetId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PetVerificationResult(Exists: true, IsOwnedByOwner: true));
        _slots.Setup(repo => repo.GetByIdAsync(slot.AvailabilitySlotId, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _veterinarians.Setup(repo => repo.GetByIdAsync(veterinarian.VeterinarianId, It.IsAny<CancellationToken>())).ReturnsAsync(veterinarian);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(slot.IsBooked);
        Assert.Equal(PetId, result.PetId);
        Assert.Equal(OwnerId, result.OwnerId);
        Assert.Equal(veterinarian.VeterinarianId, result.VeterinarianId);
        Assert.Equal(ClinicId, result.ClinicId);

        _appointments.Verify(repo => repo.AddAsync(It.Is<Appointment>(a => a.PetId == PetId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(publisher => publisher.PublishAsync(
                PetCareTopics.Appointments,
                It.Is<AppointmentScheduledEvent>(e => e.PetId == PetId && e.OwnerId == OwnerId && e.VeterinarianId == veterinarian.VeterinarianId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyPetId_ThrowsValidationExceptionBeforeAnyLookup()
    {
        var command = new ScheduleAppointmentCommand(Guid.Empty, OwnerId, Guid.NewGuid(), "Check-up");
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, CancellationToken.None));

        _petVerification.Verify(client => client.VerifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPetDoesNotExist_ThrowsKeyNotFoundExceptionAndReservesNoSlot()
    {
        var command = new ScheduleAppointmentCommand(PetId, OwnerId, Guid.NewGuid(), "Check-up");
        _petVerification.Setup(client => client.VerifyAsync(PetId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PetVerificationResult(Exists: false, IsOwnedByOwner: false));

        var handler = CreateHandler();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.HandleAsync(command, CancellationToken.None));
        _slots.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPetIsNotOwnedByOwner_ThrowsPetOwnershipException()
    {
        var command = new ScheduleAppointmentCommand(PetId, OwnerId, Guid.NewGuid(), "Check-up");
        _petVerification.Setup(client => client.VerifyAsync(PetId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PetVerificationResult(Exists: true, IsOwnedByOwner: false));

        var handler = CreateHandler();

        await Assert.ThrowsAsync<PetOwnershipException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenSlotIsAlreadyBooked_PropagatesSlotAlreadyBookedExceptionAndDoesNotSave()
    {
        var veterinarian = new Veterinarian(ClinicId, "Dr. Ana Petrova", "General Practice", "VET-001");
        var slot = new AvailabilitySlot(veterinarian.VeterinarianId, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30));
        slot.Reserve(); // already booked by someone else

        var command = new ScheduleAppointmentCommand(PetId, OwnerId, slot.AvailabilitySlotId, "Check-up");
        _petVerification.Setup(client => client.VerifyAsync(PetId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PetVerificationResult(Exists: true, IsOwnedByOwner: true));
        _slots.Setup(repo => repo.GetByIdAsync(slot.AvailabilitySlotId, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _veterinarians.Setup(repo => repo.GetByIdAsync(veterinarian.VeterinarianId, It.IsAny<CancellationToken>())).ReturnsAsync(veterinarian);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<SlotAlreadyBookedException>(() => handler.HandleAsync(command, CancellationToken.None));
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEventPublishFails_StillReturnsTheBookedAppointment()
    {
        // "Validate message production behavior where possible": a Kafka outage must not turn an
        // already-committed booking into an error response — see the try/catch in the handler.
        var veterinarian = new Veterinarian(ClinicId, "Dr. Ana Petrova", "General Practice", "VET-001");
        var slot = new AvailabilitySlot(veterinarian.VeterinarianId, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30));
        var command = new ScheduleAppointmentCommand(PetId, OwnerId, slot.AvailabilitySlotId, "Check-up");

        _petVerification.Setup(client => client.VerifyAsync(PetId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PetVerificationResult(Exists: true, IsOwnedByOwner: true));
        _slots.Setup(repo => repo.GetByIdAsync(slot.AvailabilitySlotId, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _veterinarians.Setup(repo => repo.GetByIdAsync(veterinarian.VeterinarianId, It.IsAny<CancellationToken>())).ReturnsAsync(veterinarian);
        _eventPublisher
            .Setup(publisher => publisher.PublishAsync(It.IsAny<string>(), It.IsAny<AppointmentScheduledEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kafka is unreachable"));

        var handler = CreateHandler();
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(PetId, result.PetId);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private ScheduleAppointmentHandler CreateHandler() => new(
        _appointments.Object,
        _slots.Object,
        _veterinarians.Object,
        _petVerification.Object,
        _unitOfWork.Object,
        _eventPublisher.Object,
        NullLogger<ScheduleAppointmentHandler>.Instance);
}
