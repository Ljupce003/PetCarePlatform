using Microsoft.EntityFrameworkCore;

namespace TreatmentAndNotificationService.Repository;

public class TreatmentNotificationDbContext: DbContext
{
    public TreatmentNotificationDbContext()
    {
    }

    public TreatmentNotificationDbContext(DbContextOptions<TreatmentNotificationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}