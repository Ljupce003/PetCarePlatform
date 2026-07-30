using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Persistence;

public class TreatmentDbContext : DbContext
{
    public DbSet<MedicalExamination> MedicalExaminations { get; set; }
    public DbSet<Vaccination> Vaccinations { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    

    public TreatmentDbContext()
    {
    }

    public TreatmentDbContext(DbContextOptions<TreatmentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicalExamination>(builder =>
        {
            builder.ToTable("medical_examinations");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Diagnosis).HasMaxLength(500).IsRequired();
            builder.Property(item => item.TreatmentPlan).HasMaxLength(2000).IsRequired();
            builder.Property(item => item.Medications).HasColumnType("text[]");
            builder.Property(item => item.Notes).HasMaxLength(2000);
            builder.HasIndex(item => item.PetId);
        });
        modelBuilder.Entity<Vaccination>(builder =>
        {
            builder.ToTable("vaccinations");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.VaccineName).HasMaxLength(150).IsRequired();
            builder.Property(item => item.BatchNumber).HasMaxLength(100);
            builder.HasIndex(item => new { item.PetId, item.NextDueOn });
        });
        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
            builder.Property(item => item.Message).HasMaxLength(1000).IsRequired();
            builder.Property(item => item.SourceEventId).HasMaxLength(200).IsRequired();
            builder.HasIndex(item => item.SourceEventId).IsUnique();
            builder.HasIndex(item => new { item.Status, item.ScheduledForUtc });
        });
        
        base.OnModelCreating(modelBuilder);
    }
}