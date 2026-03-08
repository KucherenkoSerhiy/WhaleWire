using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WhaleWire.Tests.Fakes;

/// <summary>
/// Captures log messages for integration test verification.
/// </summary>
public sealed class LogCaptureProvider : ILoggerProvider
{
    private readonly ConcurrentBag<string> _messages = [];

    public IReadOnlyList<string> Messages => [.. _messages];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose() { }

    private void Add(string message) => _messages.Add(message);

    private sealed class CapturingLogger(LogCaptureProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            provider.Add(message);
        }
    }
}
