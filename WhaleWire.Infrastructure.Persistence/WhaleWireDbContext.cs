using Microsoft.EntityFrameworkCore;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence;

public sealed class WhaleWireDbContext(DbContextOptions<WhaleWireDbContext> options) 
    : DbContext(options)
{
    public DbSet<BlockchainEvent> Events => Set<BlockchainEvent>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<AddressLease> AddressLeases => Set<AddressLease>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WhaleWireDbContext).Assembly);
    }
}

