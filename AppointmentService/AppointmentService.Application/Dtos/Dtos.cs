using AppointmentService.Domain.Enums;

namespace AppointmentService.Application.Dtos;

public sealed record ClinicDto(Guid ClinicId, string Name, string Location, string Address);

public sealed record VeterinarianDto(
    Guid VeterinarianId, Guid ClinicId, string FullName, string Specialization, bool IsAvailable);

public sealed record AvailableSlotDto(
    Guid AvailabilitySlotId,
    Guid VeterinarianId,
    string VeterinarianName,
    string Specialization,
    Guid ClinicId,
    string ClinicName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record AppointmentDto(
    Guid AppointmentId,
    Guid PetId,
    Guid OwnerId,
    Guid ClinicId,
    Guid VeterinarianId,
    Guid AvailabilitySlotId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Reason,
    AppointmentStatus Status,
    string? CancellationReason,
    DateTimeOffset CreatedAtUtc);
