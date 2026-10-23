using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Restaurant.Domain.Tables;

namespace Restaurant.Infrastructure.Persistence.Configurations;

public sealed class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("tables", "restaurant");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Number).HasColumnName("number").IsRequired();
        builder.Property(t => t.Capacity).HasColumnName("capacity").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(100);
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property<uint>("RowVersion").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        builder.HasIndex(t => t.Number).IsUnique();
    }
}
