using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Infrastructure.Persistence.Configurations;

public sealed class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> builder)
    {
        builder.ToTable("dishes", "restaurant");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Name)
            .HasConversion(name => name.Value, value => DishName.Create(value))
            .HasColumnName("name")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(d => d.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(d => d.NormalizedName).IsUnique();

        builder.ComplexProperty(d => d.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("price")
                .HasPrecision(18, 2);
        });

        builder.Property(d => d.Servings).HasColumnName("servings").IsRequired();

        builder.Property(d => d.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(d => d.IsActive).HasColumnName("is_active").IsRequired();

        builder.Navigation(d => d.Ingredients).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(d => d.Ingredients, ingredients =>
        {
            ingredients.ToTable("dish_ingredients", "restaurant");
            ingredients.WithOwner().HasForeignKey("DishId");
            ingredients.HasKey("DishId", nameof(DishIngredient.IngredientId));
            ingredients.Property(i => i.IngredientId).HasColumnName("ingredient_id");
            ingredients.HasOne<Ingredient>()
                .WithMany()
                .HasForeignKey(i => i.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
