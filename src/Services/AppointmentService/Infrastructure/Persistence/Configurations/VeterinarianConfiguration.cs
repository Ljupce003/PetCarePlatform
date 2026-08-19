using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public sealed class VeterinarianConfiguration : IEntityTypeConfiguration<Veterinarian>
{
    public void Configure(EntityTypeBuilder<Veterinarian> builder)
    {
        builder.ToTable("veterinarians");
        builder.HasKey(veterinarian => veterinarian.VeterinarianId);
        builder.Property(veterinarian => veterinarian.VeterinarianId).ValueGeneratedNever();

        builder.Property(veterinarian => veterinarian.FullName).HasMaxLength(150).IsRequired();
        builder.Property(veterinarian => veterinarian.Specialization).HasMaxLength(100).IsRequired();
        builder.Property(veterinarian => veterinarian.LicenseNumber).HasMaxLength(80).IsRequired();

        // Foreign-key-only relationship: the Domain layer doesn't expose a Clinic navigation
        // property on purpose, since each entity is meant to be loaded independently.
        builder.HasOne<Clinic>()
            .WithMany()
            .HasForeignKey(veterinarian => veterinarian.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
