using AppointmentService.Application.Abstractions;
using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence;

/// <summary>
/// The Appointment Service database context. This service owns its schema exclusively —
/// no other service reads or writes it.
/// </summary>
/// <remarks>
/// Entity mappings live in <c>Persistence/Configurations</c>, one <see cref="IEntityTypeConfiguration{TEntity}"/>
/// per entity, applied below via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>.
/// </remarks>
public sealed class AppointmentDbContext(DbContextOptions<AppointmentDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Veterinarian> Veterinarians => Set<Veterinarian>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppointmentDbContext).Assembly);
    }

    // Implements IUnitOfWork so every Application-layer handler that already loaded its
    // entities through this same context can commit them with a single call, without a
    // separate wrapper type. DbContext.SaveChangesAsync returns Task<int>; this explicit
    // implementation just discards the row count, since callers only care that it succeeded.
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => await SaveChangesAsync(cancellationToken);
}
