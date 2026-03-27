using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Alerts;
using WhaleWire.Infrastructure.Notifications.Notifiers;

namespace WhaleWire.Infrastructure.Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services)
    {
        services.AddScoped<IWhaleDecisionAuditLogger, WhaleDecisionAuditLogger>();
        services.AddScoped<IAlertEvaluator, AlertEvaluator>();
        services.AddScoped<IAlertNotifier, ConsoleAlertNotifier>();

        return services;
    }
}
