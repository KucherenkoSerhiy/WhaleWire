using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Configurations;

internal sealed class AddressLeaseConfiguration : IEntityTypeConfiguration<AddressLease>
{
    public void Configure(EntityTypeBuilder<AddressLease> builder)
    {
        builder.ToTable("address_leases");

        builder.HasKey(l => l.LeaseKey);

        builder.Property(l => l.LeaseKey)
            .HasColumnName("lease_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.OwnerId)
            .HasColumnName("owner_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(l => l.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.HasIndex(l => l.ExpiresAt)
            .HasDatabaseName("ix_address_leases_expires_at");
    }
}

