using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Persistence;
using WhaleWire.Infrastructure.Persistence.Repositories;

namespace WhaleWire.Infrastructure.Persistence;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPersistence(string connectionString)
        {
            services.AddDbContext<WhaleWireDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<ICheckpointRepository, CheckpointRepository>();
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ILeaseRepository, LeaseRepository>();

            return services;
        }
    }
}

