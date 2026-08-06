using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(appointment => appointment.AppointmentId);
        builder.Property(appointment => appointment.AppointmentId).ValueGeneratedNever();

        builder.Property(appointment => appointment.Reason).HasMaxLength(500).IsRequired();
        builder.Property(appointment => appointment.CancellationReason).HasMaxLength(500);

        // Stored as text ("Scheduled"/"Cancelled"/"Completed") instead of a raw int, so the
        // status is readable when inspecting the database directly during the demo.
        builder.Property(appointment => appointment.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(appointment => appointment.PetId);
        builder.HasIndex(appointment => appointment.OwnerId);

        // No navigation to AvailabilitySlot on purpose (see Appointment.Reschedule's remarks —
        // the two aggregates are coordinated by the Application layer, not by each other).
        // Restrict keeps a slot from being deleted while an appointment still references it.
        builder.HasOne<AvailabilitySlot>()
            .WithMany()
            .HasForeignKey(appointment => appointment.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
