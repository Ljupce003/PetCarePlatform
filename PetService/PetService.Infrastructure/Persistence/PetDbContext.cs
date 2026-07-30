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
public sealed class PetDbContext(DbContextOptions<PetDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => base.OnModelCreating(modelBuilder);
}
