using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("clinics");
        builder.HasKey(clinic => clinic.ClinicId);
        builder.Property(clinic => clinic.ClinicId).ValueGeneratedNever();

        builder.Property(clinic => clinic.Name).HasMaxLength(150).IsRequired();
        builder.Property(clinic => clinic.Location).HasMaxLength(100).IsRequired();
        builder.Property(clinic => clinic.Address).HasMaxLength(250).IsRequired();
    }
}
