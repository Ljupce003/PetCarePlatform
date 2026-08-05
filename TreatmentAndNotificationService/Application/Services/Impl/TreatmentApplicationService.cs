using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Infrastructure.Persistence;

namespace TreatmentAndNotificationService.Application.Services.Impl;

public class TreatmentApplicationService: ITreatmentApplicationService
{
    private readonly IMedicalExaminationRepository _medicalExaminationRepository;
    private readonly IVaccinationRepository _vaccinationRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentApplicationService(
        IMedicalExaminationRepository medicalExaminationRepository, 
        IVaccinationRepository vaccinationRepository, 
        INotificationRepository notificationRepository, 
        TreatmentDbContext context)
    {
        _medicalExaminationRepository = medicalExaminationRepository;
        _vaccinationRepository = vaccinationRepository;
        _notificationRepository = notificationRepository;
        _context = context;
    }

    public async Task<List<MedicalExaminationDto>> GetMedicalHistory(Guid petId, CancellationToken ct)
    {
        var res = await _medicalExaminationRepository.GetByPetId(petId, ct);
        return res
            .Select(MedicalExamination.ToDto)
            .ToList();
    }

    public async Task<MedicalExaminationDto> RecordExaminationAsync(RecordMedicalExaminationRequest request, CancellationToken ct)
    {
        var examination = new MedicalExamination(request.PetId, request.OwnerId, request.VeterinarianId,
            request.AppointmentId, request.ExaminedAtUtc, request.Diagnosis, request.TreatmentPlan, request.Medications,
            request.NextControlAtUtc, request.Notes);

        await _medicalExaminationRepository.AddExamination(examination, ct);

        if (request.NextControlAtUtc.HasValue)
        {
            var newNotification = new Notification(
                request.OwnerId,
                request.PetId,
                NotificationType.FollowUpReminder,
                "Veterinary follow-up",
                $"A follow-up visit is recommended on {request.NextControlAtUtc:yyyy-MM-dd HH:mm} UTC.",
                request.NextControlAtUtc.Value.AddDays(-1), 
                $"examination:{examination.Id}");
            
            await _notificationRepository.AddNotification(newNotification, ct);
        }
        
        await _context.SaveChangesAsync(ct);
        
        return MedicalExamination.ToDto(examination);
    }

    public async Task<List<VaccinationDto>> GetVaccinationsAsync(Guid petId, CancellationToken ct)
    {
        var res = await _vaccinationRepository.GetByPetId(petId, ct);
        return res
            .Select(Vaccination.ToDto)
            .ToList();
    }

    public async Task<VaccinationDto?> GetNextVaccinationAsync(Guid petId, CancellationToken ct)
    {
        var nextVac = await _vaccinationRepository.GetNextVaccinationForPet(petId, ct);
        return nextVac != null ? Vaccination.ToDto(nextVac) : null;
    }

    public async Task<VaccinationDto> RecordVaccinationAsync(RecordVaccinationRequest request, CancellationToken ct)
    {

        var vaccination = new Vaccination(request.PetId, request.OwnerId, request.VeterinarianId, request.VaccineName,
            request.AdministeredOn, request.NextDueOn, request.BatchNumber);

        await _vaccinationRepository.AddVaccination(vaccination, ct);

        if (request.NextDueOn.HasValue)
        {
            var due = new DateTimeOffset(request.NextDueOn.Value.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
            var notification = new Notification(
                request.OwnerId,
                request.PetId,
                NotificationType.VaccinationReminder,"Vaccination reminder",
                $"{request.VaccineName} is due on {request.NextDueOn:yyyy-MM-dd}.",
                due.AddDays(-7),
                $"vaccination:{vaccination.Id}"
                );

            await _notificationRepository.AddNotification(notification, ct);
        }

        await _context.SaveChangesAsync(ct);
        return Vaccination.ToDto(vaccination);
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(Guid ownerId, CancellationToken ct)
    {
        var res = await _notificationRepository.GetByOwnerId(ownerId, ct);
        return res
            .Select(Notification.ToDto)
            .ToList();
    }
}