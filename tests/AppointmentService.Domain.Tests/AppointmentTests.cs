using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;
using Xunit;

namespace AppointmentService.Domain.Tests;

public sealed class AppointmentTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesScheduledAppointment()
    {
        var appointment = ValidAppointment();

        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.NotEqual(Guid.Empty, appointment.AppointmentId);
        Assert.Null(appointment.CancellationReason);
    }

    [Fact]
    public void Constructor_WhenPetIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Appointment(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Start, End, "Check-up"));
    }

    [Fact]
    public void Constructor_WhenReasonIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Start, End, "   "));
    }

    [Fact]
    public void Constructor_WhenEndsBeforeStarts_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            End, Start, "Check-up"));
    }

    [Fact]
    public void Cancel_WhenScheduled_SetsStatusAndReason()
    {
        var appointment = ValidAppointment();

        appointment.Cancel("Owner requested cancellation");

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("Owner requested cancellation", appointment.CancellationReason);
    }

    [Fact]
    public void Cancel_WithNoReason_LeavesCancellationReasonNull()
    {
        var appointment = ValidAppointment();

        appointment.Cancel(null);

        Assert.Null(appointment.CancellationReason);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsInvalidAppointmentStatusTransitionException()
    {
        var appointment = ValidAppointment();
        appointment.Cancel("first cancellation");

        var exception = Assert.Throws<InvalidAppointmentStatusTransitionException>(() => appointment.Cancel("second"));
        Assert.Equal(appointment.AppointmentId, exception.AppointmentId);
        Assert.Equal(AppointmentStatus.Cancelled, exception.CurrentStatus);
    }

    [Fact]
    public void Reschedule_WhenScheduled_UpdatesSlotVeterinarianAndTimes()
    {
        var appointment = ValidAppointment();
        var newSlotId = Guid.NewGuid();
        var newVeterinarianId = Guid.NewGuid();
        var newStart = Start.AddDays(1);
        var newEnd = End.AddDays(1);

        appointment.Reschedule(newSlotId, newVeterinarianId, newStart, newEnd);

        Assert.Equal(newSlotId, appointment.AvailabilitySlotId);
        Assert.Equal(newVeterinarianId, appointment.VeterinarianId);
        Assert.Equal(newStart, appointment.StartsAtUtc);
        Assert.Equal(newEnd, appointment.EndsAtUtc);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
    }

    [Fact]
    public void Reschedule_WhenAlreadyCompleted_ThrowsInvalidAppointmentStatusTransitionException()
    {
        var appointment = ValidAppointment();
        appointment.Complete();

        Assert.Throws<InvalidAppointmentStatusTransitionException>(() =>
            appointment.Reschedule(Guid.NewGuid(), Guid.NewGuid(), Start.AddDays(1), End.AddDays(1)));
    }

    [Fact]
    public void Complete_WhenScheduled_SetsStatusToCompleted()
    {
        var appointment = ValidAppointment();

        appointment.Complete();

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public void Complete_WhenAlreadyCancelled_ThrowsInvalidAppointmentStatusTransitionException()
    {
        var appointment = ValidAppointment();
        appointment.Cancel(null);

        Assert.Throws<InvalidAppointmentStatusTransitionException>(appointment.Complete);
    }

    private static readonly DateTimeOffset Start = DateTimeOffset.UtcNow.AddDays(1);
    private static readonly DateTimeOffset End = Start.AddMinutes(30);

    private static Appointment ValidAppointment() => new(
        petId: Guid.NewGuid(),
        ownerId: Guid.NewGuid(),
        clinicId: Guid.NewGuid(),
        veterinarianId: Guid.NewGuid(),
        availabilitySlotId: Guid.NewGuid(),
        startsAtUtc: Start,
        endsAtUtc: End,
        reason: "Annual check-up");
}
