using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PetService.Domain.Entities;
using PetService.Domain.ValueObjects;

namespace PetService.Infrastructure.Persistence.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("pets");
        builder.HasKey(pet => pet.PetId);
        builder.Property(pet => pet.PetId).ValueGeneratedNever();

        var petNameConverter = new ValueConverter<PetName, string>(
            name => name.Value,
            value => PetName.Create(value));

        var microchipConverter = new ValueConverter<MicrochipNumber?, string?>(
            microchip => microchip == null ? null : microchip.Value,
            value => string.IsNullOrWhiteSpace(value) ? null : MicrochipNumber.Create(value));

        builder.Property(pet => pet.Name)
            .HasConversion(petNameConverter)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pet => pet.Species)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pet => pet.Breed).HasMaxLength(100);
        builder.Property(pet => pet.BirthDate).IsRequired();
        builder.Property(pet => pet.Weight).HasPrecision(8, 2).IsRequired();

        builder.Property(pet => pet.MicrochipNumber)
            .HasConversion(microchipConverter)
            .HasMaxLength(30);

        builder.Property(pet => pet.Allergies)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(pet => pet.ChronicConditions)
            .HasColumnType("text[]")
            .IsRequired();

        builder.HasIndex(pet => pet.OwnerId);
        builder.HasIndex(pet => pet.MicrochipNumber).IsUnique();

        // Deleting an owner through the explicit DeleteOwner use case removes their pets as
        // part of the same database transaction and cannot leave orphaned pet records.
        builder.HasOne<Owner>()
            .WithMany()
            .HasForeignKey(pet => pet.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
