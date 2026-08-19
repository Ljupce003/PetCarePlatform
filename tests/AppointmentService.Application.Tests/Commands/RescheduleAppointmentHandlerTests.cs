using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Commands;
using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.AppointmentEvents;
using Shared.Messaging;
using Xunit;

namespace AppointmentService.Application.Tests.Commands;

public sealed class RescheduleAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IAvailabilitySlotRepository> _slots = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IIntegrationEventPublisher> _eventPublisher = new();

    [Fact]
    public async Task HandleAsync_OntoADifferentOpenSlot_ReservesNewSlotReleasesOldOneAndPublishesEvent()
    {
        var veterinarianId = Guid.NewGuid();
        var oldStart = DateTimeOffset.UtcNow.AddDays(1);
        var oldSlot = new AvailabilitySlot(veterinarianId, oldStart, oldStart.AddMinutes(30));
        var appointment = new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), veterinarianId, oldSlot.AvailabilitySlotId,
            oldSlot.StartsAtUtc, oldSlot.EndsAtUtc, "Annual check-up");
        oldSlot.Reserve();

        var newVeterinarianId = Guid.NewGuid();
        var newStart = DateTimeOffset.UtcNow.AddDays(2);
        var newSlot = new AvailabilitySlot(newVeterinarianId, newStart, newStart.AddMinutes(30));

        var command = new RescheduleAppointmentCommand(appointment.AppointmentId, newSlot.AvailabilitySlotId);

        _appointments.Setup(repo => repo.GetByIdAsync(appointment.AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        _slots.Setup(repo => repo.GetByIdAsync(newSlot.AvailabilitySlotId, It.IsAny<CancellationToken>())).ReturnsAsync(newSlot);
        _slots.Setup(repo => repo.GetByIdAsync(oldSlot.AvailabilitySlotId, It.IsAny<CancellationToken>())).ReturnsAsync(oldSlot);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(newSlot.IsBooked);
        Assert.False(oldSlot.IsBooked);
        Assert.Equal(newSlot.AvailabilitySlotId, result.AvailabilitySlotId);
        Assert.Equal(newVeterinarianId, result.VeterinarianId);
        _eventPublisher.Verify(publisher => publisher.PublishAsync(
                PetCareTopics.Appointments,
                It.Is<AppointmentRescheduledEvent>(e => e.AppointmentId == appointment.AppointmentId && e.VeterinarianId == newVeterinarianId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OntoItsOwnCurrentSlot_IsANoOpAndDoesNotPublish()
    {
        var veterinarianId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var slot = new AvailabilitySlot(veterinarianId, start, start.AddMinutes(30));
        var appointment = new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), veterinarianId, slot.AvailabilitySlotId,
            slot.StartsAtUtc, slot.EndsAtUtc, "Annual check-up");
        var command = new RescheduleAppointmentCommand(appointment.AppointmentId, slot.AvailabilitySlotId);

        _appointments.Setup(repo => repo.GetByIdAsync(appointment.AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(slot.AvailabilitySlotId, result.AvailabilitySlotId);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(publisher => publisher.PublishAsync(
                It.IsAny<string>(), It.IsAny<AppointmentRescheduledEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenNewSlotDoesNotExist_ThrowsKeyNotFoundException()
    {
        var veterinarianId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var oldSlot = new AvailabilitySlot(veterinarianId, start, start.AddMinutes(30));
        var appointment = new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), veterinarianId, oldSlot.AvailabilitySlotId,
            oldSlot.StartsAtUtc, oldSlot.EndsAtUtc, "Annual check-up");
        var missingSlotId = Guid.NewGuid();
        var command = new RescheduleAppointmentCommand(appointment.AppointmentId, missingSlotId);

        _appointments.Setup(repo => repo.GetByIdAsync(appointment.AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        _slots.Setup(repo => repo.GetByIdAsync(missingSlotId, It.IsAny<CancellationToken>())).ReturnsAsync((AvailabilitySlot?)null);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyNewAvailabilitySlotId_ThrowsValidationException()
    {
        var command = new RescheduleAppointmentCommand(Guid.NewGuid(), Guid.Empty);
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    private RescheduleAppointmentHandler CreateHandler() => new(
        _appointments.Object,
        _slots.Object,
        _unitOfWork.Object,
        _eventPublisher.Object,
        NullLogger<RescheduleAppointmentHandler>.Instance);
}
