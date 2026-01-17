namespace WhaleWire.Configuration;

public sealed class CircuitBreakerOptions
{
    public const string SectionName = "CircuitBreaker";

    public int ExceptionsAllowedBeforeBreaking { get; init; } = 5;
    public int DurationOfBreakMinutes { get; init; } = 1;
}