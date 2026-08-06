using AppointmentService.Application.Abstractions;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence;

public sealed class ClinicRepository(AppointmentDbContext dbContext) : IClinicRepository
{
    public async Task<IReadOnlyList<Clinic>> SearchAsync(string? location, CancellationToken cancellationToken)
    {
        var query = dbContext.Clinics.AsQueryable();

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(clinic => EF.Functions.ILike(clinic.Location, $"%{location}%"));
        }

        return await query.OrderBy(clinic => clinic.Name).ToListAsync(cancellationToken);
    }
}

public sealed class VeterinarianRepository(AppointmentDbContext dbContext) : IVeterinarianRepository
{
    public async Task<Veterinarian?> GetByIdAsync(Guid veterinarianId, CancellationToken cancellationToken) =>
        await dbContext.Veterinarians.FirstOrDefaultAsync(veterinarian => veterinarian.VeterinarianId == veterinarianId, cancellationToken);

    public async Task<IReadOnlyList<Veterinarian>> SearchAsync(Guid? clinicId, string? specialization, CancellationToken cancellationToken)
    {
        var query = dbContext.Veterinarians.AsQueryable();

        if (clinicId is { } id)
        {
            query = query.Where(veterinarian => veterinarian.ClinicId == id);
        }

        if (!string.IsNullOrWhiteSpace(specialization))
        {
            query = query.Where(veterinarian => EF.Functions.ILike(veterinarian.Specialization, $"%{specialization}%"));
        }

        return await query.OrderBy(veterinarian => veterinarian.FullName).ToListAsync(cancellationToken);
    }
}

public sealed class AvailabilitySlotRepository(AppointmentDbContext dbContext) : IAvailabilitySlotRepository
{
    public async Task<AvailabilitySlot?> GetByIdAsync(Guid availabilitySlotId, CancellationToken cancellationToken) =>
        await dbContext.AvailabilitySlots.FirstOrDefaultAsync(slot => slot.AvailabilitySlotId == availabilitySlotId, cancellationToken);

    public async Task<IReadOnlyList<AvailableSlotSearchResult>> SearchAvailableAsync(
        Guid? veterinarianId, DateOnly? date, CancellationToken cancellationToken)
    {
        var query =
            from slot in dbContext.AvailabilitySlots
            join veterinarian in dbContext.Veterinarians on slot.VeterinarianId equals veterinarian.VeterinarianId
            join clinic in dbContext.Clinics on veterinarian.ClinicId equals clinic.ClinicId
            where !slot.IsBooked && slot.StartsAtUtc > DateTimeOffset.UtcNow
            select new { slot, veterinarian, clinic };

        if (veterinarianId is { } vetId)
        {
            query = query.Where(row => row.veterinarian.VeterinarianId == vetId);
        }

        if (date is { } day)
        {
            var startOfDay = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var endOfDay = startOfDay.AddDays(1);
            query = query.Where(row => row.slot.StartsAtUtc >= startOfDay && row.slot.StartsAtUtc < endOfDay);
        }

        var rows = await query.OrderBy(row => row.slot.StartsAtUtc).ToListAsync(cancellationToken);

        return rows
            .Select(row => new AvailableSlotSearchResult(
                row.slot.AvailabilitySlotId,
                row.veterinarian.VeterinarianId,
                row.veterinarian.FullName,
                row.veterinarian.Specialization,
                row.clinic.ClinicId,
                row.clinic.Name,
                row.slot.StartsAtUtc,
                row.slot.EndsAtUtc))
            .ToList();
    }
}

public sealed class AppointmentRepository(AppointmentDbContext dbContext) : IAppointmentRepository
{
    public async Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken) =>
        await dbContext.Appointments.FirstOrDefaultAsync(appointment => appointment.AppointmentId == appointmentId, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetUpcomingByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Where(appointment => appointment.OwnerId == ownerId
                && appointment.Status == AppointmentStatus.Scheduled
                && appointment.StartsAtUtc >= DateTimeOffset.UtcNow)
            .OrderBy(appointment => appointment.StartsAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken) =>
        await dbContext.Appointments.AddAsync(appointment, cancellationToken);
}
