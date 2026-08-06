using PetService.Application.Abstractions;
using PetService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PetService.Infrastructure.Persistence;

/// <summary>
/// The Pet Service database context. This service owns its schema exclusively —
/// no other service reads or writes it.
/// </summary>
/// <remarks>
/// Entity sets and mappings arrive with the Pet domain model. Each entity gets its own
/// <see cref="IEntityTypeConfiguration{TEntity}"/> in this assembly, applied here via
/// <c>modelBuilder.ApplyConfigurationsFromAssembly(...)</c> once the first one exists.
/// </remarks>
public class PetDbContext(DbContextOptions<PetDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<Pet> Pets => Set<Pet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PetDbContext).Assembly);
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);
}
