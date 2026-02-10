namespace WhaleWire.Application.Alerts;

public interface IAlertNotifier
{
    Task NotifyAsync(Alert alert, CancellationToken ct = default);
}
