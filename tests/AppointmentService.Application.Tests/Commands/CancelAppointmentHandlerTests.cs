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

public sealed class CancelAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IAvailabilitySlotRepository> _slots = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IIntegrationEventPublisher> _eventPublisher = new();

    [Fact]
    public async Task HandleAsync_WhenScheduled_CancelsAndReleasesSlotAndPublishesEvent()
    {
        var (appointment, slot) = ScheduledAppointmentWithSlot();
        var command = new CancelAppointmentCommand(appointment.AppointmentId, "Owner requested cancellation");

        _appointments.Setup(repo => repo.GetByIdAsync(appointment.AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        _slots.Setup(repo => repo.GetByIdAsync(slot.AvailabilitySlotId, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        slot.Reserve(); // it was booked when the appointment was made

        var handler = CreateHandler();
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(slot.IsBooked);
        Assert.Equal("Owner requested cancellation", result.CancellationReason);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(publisher => publisher.PublishAsync(
                PetCareTopics.Appointments,
                It.Is<AppointmentCancelledEvent>(e => e.AppointmentId == appointment.AppointmentId && e.CancellationReason == "Owner requested cancellation"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenAppointmentDoesNotExist_ThrowsKeyNotFoundException()
    {
        var command = new CancelAppointmentCommand(Guid.NewGuid(), null);
        _appointments.Setup(repo => repo.GetByIdAsync(command.AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyCancelled_ThrowsInvalidAppointmentStatusTransitionException()
    {
        var (appointment, slot) = ScheduledAppointmentWithSlot();
        appointment.Cancel("first cancellation");
        var command = new CancelAppointmentCommand(appointment.AppointmentId, "second attempt");

        _appointments.Setup(repo => repo.GetByIdAsync(appointment.AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        _slots.Setup(repo => repo.GetByIdAsync(slot.AvailabilitySlotId, It.IsAny<CancellationToken>())).ReturnsAsync(slot);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidAppointmentStatusTransitionException>(() => handler.HandleAsync(command, CancellationToken.None));
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyAppointmentId_ThrowsValidationException()
    {
        var command = new CancelAppointmentCommand(Guid.Empty, null);
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    private static (Appointment Appointment, AvailabilitySlot Slot) ScheduledAppointmentWithSlot()
    {
        var veterinarianId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var slot = new AvailabilitySlot(veterinarianId, start, start.AddMinutes(30));
        var appointment = new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), veterinarianId, slot.AvailabilitySlotId,
            slot.StartsAtUtc, slot.EndsAtUtc, "Annual check-up");
        return (appointment, slot);
    }

    private CancelAppointmentHandler CreateHandler() => new(
        _appointments.Object,
        _slots.Object,
        _unitOfWork.Object,
        _eventPublisher.Object,
        NullLogger<CancelAppointmentHandler>.Instance);
}
