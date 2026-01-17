using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Configurations;

internal sealed class MonitoredAddressConfiguration : IEntityTypeConfiguration<MonitoredAddress>
{
    public void Configure(EntityTypeBuilder<MonitoredAddress> builder)
    {
        builder.ToTable("monitored_addresses");

        builder.HasKey(m => new { m.Chain, m.Address, m.Provider });

        builder.Property(m => m.Chain)
            .HasColumnName("chain")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.Address)
            .HasColumnName("address")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(m => m.Provider)
            .HasColumnName("provider")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.Balance)
            .HasColumnName("balance")
            .IsRequired();

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(m => m.DiscoveredAt)
            .HasColumnName("discovered_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(m => m.IsActive);
        builder.HasIndex(m => m.Balance);
    }
}
