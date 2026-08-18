using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence.Configurations;

public sealed class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("availability_slots");
        builder.HasKey(slot => slot.AvailabilitySlotId);
        builder.Property(slot => slot.AvailabilitySlotId).ValueGeneratedNever();

        // Database-level backstop for the same "no two slots starting at once for one vet"
        // rule that AvailabilitySlot.Reserve() already enforces in memory.
        builder.HasIndex(slot => new { slot.VeterinarianId, slot.StartsAtUtc }).IsUnique();

        builder.HasOne<Veterinarian>()
            .WithMany()
            .HasForeignKey(slot => slot.VeterinarianId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
