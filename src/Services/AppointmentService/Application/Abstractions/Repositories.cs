using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Abstractions;

public interface IClinicRepository
{
    Task<IReadOnlyList<Clinic>> SearchAsync(string? location, CancellationToken cancellationToken);
}

public interface IVeterinarianRepository
{
    Task<Veterinarian?> GetByIdAsync(Guid veterinarianId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Veterinarian>> SearchAsync(Guid? clinicId, string? specialization, CancellationToken cancellationToken);

    Task AddAsync(Veterinarian veterinarian, CancellationToken cancellationToken);

    Task<bool> HasAppointmentsAsync(Guid veterinarianId, CancellationToken cancellationToken);

    void Remove(Veterinarian veterinarian);
}

/// <summary>
/// A read-only projection joining an open <see cref="AvailabilitySlot"/> with its veterinarian
/// and clinic, so the query side never has to load full aggregates just to list them.
/// </summary>
public sealed record AvailableSlotSearchResult(
    Guid AvailabilitySlotId,
    Guid VeterinarianId,
    string VeterinarianName,
    string Specialization,
    Guid ClinicId,
    string ClinicName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public interface IAvailabilitySlotRepository
{
    Task<AvailabilitySlot?> GetByIdAsync(Guid availabilitySlotId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AvailableSlotSearchResult>> SearchAvailableAsync(
        Guid? veterinarianId, DateOnly? date, CancellationToken cancellationToken);

    Task AddAsync(AvailabilitySlot slot, CancellationToken cancellationToken);
    void Remove(AvailabilitySlot slot);
}

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Appointment>> GetUpcomingByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Appointment>> GetUpcomingByVeterinarianAsync(Guid veterinarianId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Appointment>> GetClinicalHistoryByVeterinarianAsync(Guid veterinarianId, CancellationToken cancellationToken);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
}

/// <summary>
/// Persists everything a use case changed, in one atomic save. Handlers call this exactly once,
/// after every entity involved (appointment, slot, ...) has already applied its own rules.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
