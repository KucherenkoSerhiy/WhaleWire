using Microsoft.EntityFrameworkCore;
using WhaleWire.Infrastructure.Persistence;

namespace WhaleWire.Tests.Common.Fixtures;

public abstract class InMemoryDbContextFixture : IAsyncDisposable
{
    protected WhaleWireDbContext Context { get; }

    protected InMemoryDbContextFixture()
    {
        var options = new DbContextOptionsBuilder<WhaleWireDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        Context = new WhaleWireDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}