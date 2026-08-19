using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Exceptions;
using Xunit;

namespace AppointmentService.Domain.Tests;

public sealed class AvailabilitySlotTests
{
    private static readonly Guid VeterinarianId = Guid.NewGuid();

    [Fact]
    public void Constructor_WhenEndsBeforeStarts_Throws()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var end = start.AddMinutes(-30);

        Assert.Throws<ArgumentException>(() => new AvailabilitySlot(VeterinarianId, start, end));
    }

    [Fact]
    public void Constructor_WhenVeterinarianIdIsEmpty_Throws()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);

        Assert.Throws<ArgumentException>(() => new AvailabilitySlot(Guid.Empty, start, start.AddMinutes(30)));
    }

    [Fact]
    public void Reserve_WhenSlotIsOpenAndInTheFuture_MarksItBooked()
    {
        var slot = FutureSlot();

        slot.Reserve();

        Assert.True(slot.IsBooked);
    }

    [Fact]
    public void Reserve_WhenAlreadyBooked_ThrowsSlotAlreadyBookedException()
    {
        var slot = FutureSlot();
        slot.Reserve();

        var exception = Assert.Throws<SlotAlreadyBookedException>(slot.Reserve);
        Assert.Equal(slot.AvailabilitySlotId, exception.SlotId);
    }

    [Fact]
    public void Reserve_WhenSlotHasAlreadyStarted_ThrowsSlotExpiredException()
    {
        // AvailabilitySlot's own constructor doesn't forbid past times (only End > Start), so a
        // slot can legitimately end up "expired" by the time Reserve() is called.
        var start = DateTimeOffset.UtcNow.AddMinutes(-10);
        var slot = new AvailabilitySlot(VeterinarianId, start, start.AddMinutes(30));

        var exception = Assert.Throws<SlotExpiredException>(slot.Reserve);
        Assert.Equal(slot.AvailabilitySlotId, exception.SlotId);
    }

    [Fact]
    public void Release_AfterReserve_MakesSlotBookableAgain()
    {
        var slot = FutureSlot();
        slot.Reserve();

        slot.Release();

        Assert.False(slot.IsBooked);
        slot.Reserve(); // should no longer throw SlotAlreadyBookedException
        Assert.True(slot.IsBooked);
    }

    private static AvailabilitySlot FutureSlot()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        return new AvailabilitySlot(VeterinarianId, start, start.AddMinutes(30));
    }
}
