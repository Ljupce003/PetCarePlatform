using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence;

/// <summary>
/// Ensures the Appointment Service database exists and, the first time it's empty, seeds a
/// small set of clinics, veterinarians and availability slots — plus one already-booked
/// appointment — so the full booking workflow can be demoed without depending on the Pet
/// Service or a manually populated database.
/// </summary>
public static class AppointmentDbInitializer
{
    // Fixed ids, so a demo/presentation (or the .http file) can reference a clinic, veterinarian
    // or owner directly instead of having to search for one first.
    public static readonly Guid DemoClinicId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DemoSecondClinicId = new("11111111-1111-1111-1111-111111111112");
    public static readonly Guid DemoVeterinarianId = new("22222222-2222-2222-2222-222222222221");
    public static readonly Guid DemoSecondVeterinarianId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DemoThirdVeterinarianId = new("22222222-2222-2222-2222-222222222223");
    public static readonly Guid DemoOwnerId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DemoPetId = new("44444444-4444-4444-4444-444444444444");

    public static async Task InitializeAsync(AppointmentDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // Applies the InitialCreate migration (and any later ones) instead of EnsureCreated,
        // so the schema on disk always matches what's under Migrations/.
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedIfEmptyAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// The seeding half of <see cref="InitializeAsync"/>, split out so integration tests can call
    /// it directly against an EF Core InMemory database created with <c>EnsureCreated</c> --
    /// <c>MigrateAsync</c> above isn't supported by the InMemory provider.
    /// </summary>
    public static async Task SeedIfEmptyAsync(AppointmentDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Clinics.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedAsync(dbContext, cancellationToken);
    }

    private static async Task SeedAsync(AppointmentDbContext dbContext, CancellationToken cancellationToken)
    {
        var clinic = Clinic.Seed(DemoClinicId, "Central Vet Clinic", "Skopje", "Bul. Ilinden 1");
        var secondClinic = Clinic.Seed(DemoSecondClinicId, "North Animal Hospital", "Skopje", "Ul. Vasil Glavinov 10");
        dbContext.Clinics.AddRange(clinic, secondClinic);

        var vet = Veterinarian.Seed(DemoVeterinarianId, clinic.ClinicId, "Dr. Ana Petrova", "General Practice", "VET-001");
        var secondVet = Veterinarian.Seed(DemoSecondVeterinarianId, clinic.ClinicId, "Dr. Marko Ivanov", "Surgery", "VET-002");
        var thirdVet = Veterinarian.Seed(DemoThirdVeterinarianId, secondClinic.ClinicId, "Dr. Elena Trajkova", "Dermatology", "VET-003");
        dbContext.Veterinarians.AddRange(vet, secondVet, thirdVet);

        // Three open slots per veterinarian, spread over the next three days, so
        // SearchAvailableSlots has something to return for every seeded veterinarian.
        var today = DateTime.UtcNow.Date;
        var slots = new List<AvailabilitySlot>();
        foreach (var veterinarian in new[] { vet, secondVet, thirdVet })
        {
            for (var day = 1; day <= 3; day++)
            {
                var morning = new DateTimeOffset(today.AddDays(day).AddHours(9), TimeSpan.Zero);
                var afternoon = new DateTimeOffset(today.AddDays(day).AddHours(14), TimeSpan.Zero);
                slots.Add(new AvailabilitySlot(veterinarian.VeterinarianId, morning, morning.AddMinutes(30)));
                slots.Add(new AvailabilitySlot(veterinarian.VeterinarianId, afternoon, afternoon.AddMinutes(30)));
            }
        }

        dbContext.AvailabilitySlots.AddRange(slots);

        // Pre-book the very first slot, so GetUpcomingAppointments(DemoOwnerId) has something to
        // return without anyone having to call ScheduleAppointment first.
        var firstSlot = slots[0];
        firstSlot.Reserve();
        var demoAppointment = new Appointment(
            DemoPetId, DemoOwnerId, vet.ClinicId, firstSlot.VeterinarianId, firstSlot.AvailabilitySlotId,
            firstSlot.StartsAtUtc, firstSlot.EndsAtUtc, "Annual check-up");
        dbContext.Appointments.Add(demoAppointment);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
