using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Configurations;

internal sealed class BlockchainEventConfiguration : IEntityTypeConfiguration<BlockchainEvent>
{
    public void Configure(EntityTypeBuilder<BlockchainEvent> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Chain)
            .HasColumnName("chain")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Address)
            .HasColumnName("address")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Lt)
            .HasColumnName("lt")
            .IsRequired();

        builder.Property(e => e.TxHash)
            .HasColumnName("tx_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.BlockTime)
            .HasColumnName("block_time")
            .IsRequired();

        builder.Property(e => e.RawJson)
            .HasColumnName("raw_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(e => e.EventId)
            .IsUnique()
            .HasDatabaseName("ix_events_event_id");

        builder.HasIndex(e => new { e.Chain, e.Address, e.Lt })
            .HasDatabaseName("ix_events_chain_address_lt");
    }
}

