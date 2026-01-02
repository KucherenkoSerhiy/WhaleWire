using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Configurations;

internal sealed class CheckpointConfiguration : IEntityTypeConfiguration<Checkpoint>
{
    public void Configure(EntityTypeBuilder<Checkpoint> builder)
    {
        builder.ToTable("checkpoints");

        builder.HasKey(c => new { c.Chain, c.Address, c.Provider });

        builder.Property(c => c.Chain)
            .HasColumnName("chain")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.Address)
            .HasColumnName("address")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(c => c.Provider)
            .HasColumnName("provider")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(c => c.LastLt)
            .HasColumnName("last_lt")
            .IsRequired();

        builder.Property(c => c.LastHash)
            .HasColumnName("last_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");
    }
}

