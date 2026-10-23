using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;
using Restaurant.Infrastructure.Identity;

namespace Restaurant.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "restaurant");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.TableId).HasColumnName("table_id").IsRequired();
        builder.Property(o => o.WaiterId).HasColumnName("waiter_id").IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.ComplexProperty(o => o.Subtotal, subtotal =>
        {
            subtotal.Property(m => m.Amount)
                .HasColumnName("subtotal")
                .HasPrecision(18, 2);
        });

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.CompletedAt).HasColumnName("completed_at");

        builder.Property<uint>("RowVersion").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        builder.HasOne<Table>()
            .WithMany()
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.WaiterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.TableId, o.Status });

        builder.HasIndex(o => o.TableId)
            .IsUnique()
            .HasFilter("status = 'InProgress'");

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(o => o.Items, items =>
        {
            items.ToTable("order_items", "restaurant");
            items.WithOwner().HasForeignKey("OrderId");
            items.HasKey(i => i.Id);
            items.Property(i => i.Id).ValueGeneratedNever();

            items.Property(i => i.DishId).HasColumnName("dish_id").IsRequired();
            items.Property(i => i.DishName).HasColumnName("dish_name").HasMaxLength(80).IsRequired();
            items.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();

            items.Property(i => i.UnitPrice)
                .HasConversion(price => price.Amount, amount => Money.Create(amount))
                .HasColumnName("unit_price")
                .HasPrecision(18, 2);

            items.Ignore(i => i.LineTotal);

            items.HasOne<Dish>()
                .WithMany()
                .HasForeignKey(i => i.DishId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
