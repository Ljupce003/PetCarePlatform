using AppointmentService.Application.Abstractions;
using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Dtos;

public static class Mappings
{
    public static ClinicDto ToDto(this Clinic clinic) =>
        new(clinic.ClinicId, clinic.Name, clinic.Location, clinic.Address);

    public static VeterinarianDto ToDto(this Veterinarian veterinarian) =>
        new(veterinarian.VeterinarianId, veterinarian.ClinicId, veterinarian.FullName,
            veterinarian.Specialization, veterinarian.IsAvailable);

    public static AvailableSlotDto ToDto(this AvailableSlotSearchResult result) =>
        new(result.AvailabilitySlotId, result.VeterinarianId, result.VeterinarianName,
            result.Specialization, result.ClinicId, result.ClinicName, result.StartsAtUtc, result.EndsAtUtc);

    public static AppointmentDto ToDto(this Appointment appointment) =>
        new(appointment.AppointmentId, appointment.PetId, appointment.OwnerId, appointment.ClinicId,
            appointment.VeterinarianId, appointment.AvailabilitySlotId, appointment.StartsAtUtc,
            appointment.EndsAtUtc, appointment.Reason, appointment.Status, appointment.CancellationReason,
            appointment.CreatedAtUtc);
}
