using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;

namespace TreatmentAndNotificationService.Infrastructure.Persistence;

public static class TreatmentDbContextSeeder
{
    public static class SeedIds
    {
        public static readonly Guid Owner1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid Owner2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static readonly Guid Pet1Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"); // Dog (Max)
        public static readonly Guid Pet2Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"); // Cat (Luna)

        public static readonly Guid Vet1Id = Guid.Parse("b1111111-1111-1111-1111-111111111111"); // Dr. Smith
        public static readonly Guid Vet2Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"); // Dr. Johnson

        public static readonly Guid Appointment1Id = Guid.Parse("c1111111-1111-1111-1111-111111111111");
        public static readonly Guid Appointment2Id = Guid.Parse("c2222222-2222-2222-2222-222222222222");
    }

    public static async Task SeedAsync(TreatmentDbContext context)
    {
        if (await context.MedicalExaminations.AnyAsync() || 
            await context.Vaccinations.AnyAsync() || 
            await context.Notifications.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // --- 1. Medical Examinations ---
        var examinations = new List<MedicalExamination>
        {
            new(
                petId: SeedIds.Pet1Id,
                ownerId: SeedIds.Owner1Id,
                veterinarianId: SeedIds.Vet1Id,
                appointmentId: SeedIds.Appointment1Id,
                examinedAtUtc: now.AddDays(-10),
                diagnosis: "Acute Otitis Externa (Right Ear)",
                treatmentPlan: "Clean ear canal daily with saline solution. Apply Otomax drops twice daily for 7 days.",
                medications: new[] { "Otomax Ear Drops 15ml", "Carprofen 75mg" },
                nextControlAtUtc: now.AddDays(4),
                notes: "Patient was cooperative. Re-evaluate ear canal redness during follow-up."
            ),
            new(
                petId: SeedIds.Pet2Id,
                ownerId: SeedIds.Owner2Id,
                veterinarianId: SeedIds.Vet2Id,
                appointmentId: SeedIds.Appointment2Id,
                examinedAtUtc: now.AddDays(-30),
                diagnosis: "Mild Periodontal Disease (Stage 1)",
                treatmentPlan: "Daily dental brushing and enzymatic dental chews. Schedule professional cleaning if tartar worsens.",
                medications: new[] { "Enzymatic Toothpaste" },
                nextControlAtUtc: now.AddMonths(6),
                notes: "Gums slightly inflamed around upper molars."
            ),
            new(
                petId: SeedIds.Pet1Id,
                ownerId: SeedIds.Owner1Id,
                veterinarianId: SeedIds.Vet1Id,
                appointmentId: null,
                examinedAtUtc: now.AddMonths(-3),
                diagnosis: "Routine Annual Wellness Exam",
                treatmentPlan: "Maintain current diet and regular daily exercise.",
                medications: null,
                nextControlAtUtc: now.AddMonths(9),
                notes: "Weight stable at 28.5 kg. Heart and lungs clear."
            )
        };

        // --- 2. Vaccinations ---
        var vaccinations = new List<Vaccination>
        {
            new(
                petId: SeedIds.Pet1Id,
                ownerId: SeedIds.Owner1Id,
                veterinarianId: SeedIds.Vet1Id,
                vaccineName: "Rabies (Defensor 3)",
                administeredOn: today.AddMonths(-6),
                nextDueOn: today.AddMonths(18),
                batchNumber: "RAB-2025-089A"
            ),
            new(
                petId: SeedIds.Pet1Id,
                ownerId: SeedIds.Owner1Id,
                veterinarianId: SeedIds.Vet1Id,
                vaccineName: "DHPP (Canine Distemper/Parvo)",
                administeredOn: today.AddMonths(-1),
                nextDueOn: today.AddMonths(11),
                batchNumber: "DHPP-99412-B"
            ),
            new(
                petId: SeedIds.Pet2Id,
                ownerId: SeedIds.Owner2Id,
                veterinarianId: SeedIds.Vet2Id,
                vaccineName: "FVRCP (Feline Viral Rhinotracheitis)",
                administeredOn: today.AddDays(-20),
                nextDueOn: today.AddDays(345),
                batchNumber: "FVR-44102-C"
            )
        };

        // --- 3. Notifications ---
        var notification1 = new Notification(
            ownerId: SeedIds.Owner1Id,
            petId: SeedIds.Pet1Id,
            type: Enum.GetValues<NotificationType>().First(), // Replace with your exact enum value e.g. NotificationType.FollowUp
            title: "Upcoming Control Exam Reminder",
            message: "Reminder: Max has a scheduled control examination for ear infection check.",
            scheduledForUtc: now.AddDays(3),
            sourceEventId: $"EVENT-EXAM-CONTROL-{examinations[0].Id}"
        );

        var notification2 = new Notification(
            ownerId: SeedIds.Owner1Id,
            petId: SeedIds.Pet1Id,
            type: Enum.GetValues<NotificationType>().First(), // Replace with your exact enum value e.g. NotificationType.VaccineReminder
            title: "DHPP Vaccination Recorded",
            message: "Max received the annual DHPP booster vaccination.",
            scheduledForUtc: now.AddMonths(-1),
            sourceEventId: $"EVENT-VAC-CONFIRM-{vaccinations[1].Id}"
        );
        notification2.MarkSent();

        var notification3 = new Notification(
            ownerId: SeedIds.Owner2Id,
            petId: SeedIds.Pet2Id,
            type: Enum.GetValues<NotificationType>().First(),
            title: "FVRCP Vaccination Recorded",
            message: "Luna received the annual FVRCP vaccination.",
            scheduledForUtc: now.AddDays(-20),
            sourceEventId: $"EVENT-VAC-CONFIRM-{vaccinations[2].Id}"
        );
        notification3.MarkSent();

        var notifications = new List<Notification> { notification1, notification2, notification3 };

        await context.MedicalExaminations.AddRangeAsync(examinations);
        await context.Vaccinations.AddRangeAsync(vaccinations);
        await context.Notifications.AddRangeAsync(notifications);

        await context.SaveChangesAsync();
    }
}