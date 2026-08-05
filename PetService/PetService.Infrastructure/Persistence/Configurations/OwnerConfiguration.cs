using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetService.Domain.Entities;

namespace PetService.Infrastructure.Persistence.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("owners");
        builder.HasKey(owner => owner.OwnerId);
        builder.Property(owner => owner.OwnerId).ValueGeneratedNever();

        builder.Property(owner => owner.OwnerName).HasMaxLength(100).IsRequired();
        builder.Property(owner => owner.Email).HasMaxLength(254).IsRequired();
        builder.Property(owner => owner.Phone).HasMaxLength(30).IsRequired();
        builder.Property(owner => owner.Address).HasMaxLength(500);

        builder.HasIndex(owner => owner.Email);
    }
}
