namespace WhaleWire.Configuration;

public sealed class SchedulerOptions
{
    public const string SectionName = "Scheduler";

    public bool Enabled { get; init; } = true;
    public int PollingIntervalSeconds { get; init; } = 60;
}