using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Persistence;
using WhaleWire.Infrastructure.Persistence.Repositories;

namespace WhaleWire.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<WhaleWireDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ICheckpointRepository, CheckpointRepository>();
        services.AddScoped<ILeaseRepository, LeaseRepository>();

        return services;
    }
}

