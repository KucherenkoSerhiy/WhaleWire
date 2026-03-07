using System.Diagnostics;
using System.Reflection;
using FluentAssertions;

namespace WhaleWire.Tests.Integration;

/// <summary>
/// Validates Prometheus alert rules load and parse correctly.
/// Runs promtool via Docker (same runtime as docker-compose).
/// </summary>
public sealed class PrometheusRulesValidationTests
{
    private static string GetPrometheusAlertsPath()
    {
        var dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(dir, "prometheus", "alerts", "whalewire.yml");
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        dir = assemblyDir;
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(dir, "prometheus", "alerts", "whalewire.yml");
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("prometheus/alerts/whalewire.yml not found");
    }

    [Fact]
    public async Task PrometheusRules_AreValid_PromtoolCheckPasses()
    {
        var rulesPath = GetPrometheusAlertsPath();
        var rulesDir = Path.GetDirectoryName(rulesPath)!;

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList = { "run", "--rm", "--entrypoint", "promtool", "-v", $"{rulesDir}:/rules:ro", "prom/prometheus:v3.0.0", "check", "rules", "/rules/whalewire.yml" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process.Should().NotBeNull();
        var stdout = await process!.StandardOutput.ReadToEndAsync();
        var stderr = await process!.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = stdout + stderr;
        process.ExitCode.Should().Be(0, "promtool check rules should pass. Output: {0}", output);
    }
}
