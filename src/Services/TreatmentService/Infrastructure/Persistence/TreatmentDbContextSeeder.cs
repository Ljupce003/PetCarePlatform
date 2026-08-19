using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Infrastructure.Persistence;

/// <summary>Development-only data. Production data is never seeded by the application.</summary>
public static class TreatmentDbContextSeeder
{
    public static async Task SeedAsync(TreatmentDbContext context)
    {
        if (await context.MedicalExaminations.AnyAsync()) return;

        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var petId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        var veterinarianId = Guid.Parse("b1111111-1111-1111-1111-111111111111");
        var examinedAt = DateTimeOffset.UtcNow.AddDays(-10);
        var examination = new MedicalExamination(petId, ownerId, veterinarianId, null, examinedAt,
            Diagnosis.Create("Routine wellness examination"), TreatmentPlan.Create("Maintain regular exercise and a balanced diet."),
            ["Annual parasite prevention"], DateTimeOffset.UtcNow.AddDays(14), "Patient is in good condition.");
        var vaccination = new Vaccination(petId, ownerId, veterinarianId, VaccineName.Create("Rabies"),
            VaccinationSchedule.Create(DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6))),
            "RAB-DEMO-001");

        context.MedicalExaminations.Add(examination);
        context.Vaccinations.Add(vaccination);
        context.Notifications.Add(new Notification(ownerId, petId, NotificationType.FollowUpReminder,
            NotificationContent.Create("Upcoming control exam", "A follow-up veterinary visit is scheduled soon."),
            DateTimeOffset.UtcNow.AddDays(13), SourceEventId.Create("seed:follow-up:1")));
        await context.SaveChangesAsync();
    }
}
