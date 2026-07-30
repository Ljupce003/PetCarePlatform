using Microsoft.EntityFrameworkCore;

namespace PetCarePlatform.AppointmentService.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the Appointment bounded context. It is backed by its
/// own PostgreSQL database (see ConnectionStrings:Database), isolated from every other
/// microservice's database, per the platform's "database per service" rule.
/// </summary>
public sealed class AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : DbContext(options)
{
    // DbSet<T> properties for Clinic, Veterinarian, AvailabilitySlot and Appointment
    // are added here once the Appointment domain model is implemented.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("appointment");
    }
}
