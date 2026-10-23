using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Restaurant.Domain.Ingredients;

namespace Restaurant.Infrastructure.Persistence.Configurations;

public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients", "restaurant");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Name)
            .HasConversion(name => name.Value, value => IngredientName.Create(value))
            .HasColumnName("name")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(i => i.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(60)
            .IsRequired();

        builder.HasIndex(i => i.NormalizedName).IsUnique();
    }
}
