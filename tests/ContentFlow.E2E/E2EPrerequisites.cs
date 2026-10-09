// Environment probes for the E2E suite (ADR-007).
//
// xUnit 2.9.3 (pinned in Directory.Packages.props) has NO runtime-skip API:
//  - Assert.Skip does not exist,
//  - Xunit.Sdk.SkipException has no public constructor,
//  - throwing a SkipException instance is reported as Failed, not Skipped
//    (verified empirically against xunit 2.9.3 on .NET 10).
// Therefore gating is done at DISCOVERY time via FactAttribute subclasses
// (RequiresDockerFact / RequiresChromiumFact) that set FactAttribute.Skip.
// The probes below are deliberately cheap filesystem/pipe checks — no image
// pulls, no browser launches — so discovery stays fast and deterministic.
// If main prefers Skip.If-style runtime skips, add the Xunit.SkippableFact
// package and replace the attributes (see package lines in the task report).

using System.IO.Pipes;

namespace ContentFlow.E2E;

/// <summary>
/// Cheap, side-effect-free probes for E2E prerequisites plus the human-readable
/// reasons/install guidance surfaced when a prerequisite is missing.
/// </summary>
public static class E2EPrerequisites
{
    /// <summary>
    /// True when a Docker endpoint appears reachable: Windows named pipe,
    /// Unix socket, or an explicit DOCKER_HOST. Does NOT pull images or start
    /// containers — that happens in <see cref="EphemeralEnvironment"/>.
    /// </summary>
    public static bool IsDockerEndpointPresent()
    {
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            return true;
        }

        if (OperatingSystem.IsWindows())
        {
            // The default Docker Desktop / Engine named pipe. A short connect
            // proves the pipe exists without sending any Docker API traffic.
            try
            {
                using var pipe = new NamedPipeClientStream(
                    ".", "docker_engine", PipeDirection.InOut, PipeOptions.Asynchronous);
                var connect = pipe.ConnectAsync(1000);
                return connect.Wait(1500) && pipe.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        return File.Exists("/var/run/docker.sock");
    }

    /// <summary>
    /// True when a Playwright Chromium build appears installed (browser cache
    /// populated). Does NOT launch a browser — that happens in
    /// <see cref="BrowserFixture"/> on first use.
    /// </summary>
    public static bool IsChromiumPresent()
    {
        foreach (var baseDir in PlaywrightCacheDirs())
        {
            try
            {
                if (Directory.Exists(baseDir)
                    && Directory.GetDirectories(baseDir, "chromium-*").Length > 0)
                {
                    return true;
                }
            }
            catch
            {
                // Treat unreadable cache dirs as absent; launch will fail
                // closed later with actionable guidance.
            }
        }

        return false;
    }

    public const string DockerMissingReason =
        "E2E prerequisite unavailable: no Docker endpoint found. " +
        "Start Docker Desktop (or set DOCKER_HOST) and re-run. " +
        "Container-backed tests are skipped; HTTP tests still run.";

    public const string ChromiumMissingReason =
        "E2E prerequisite unavailable: Playwright Chromium browsers are not installed. " +
        "Install once per machine with: " +
        "pwsh tests/ContentFlow.E2E/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium " +
        "(run 'dotnet build tests/ContentFlow.E2E' first so playwright.ps1 is emitted). " +
        "Browser tests are skipped; HTTP tests still run.";

    private static IEnumerable<string> PlaywrightCacheDirs()
    {
        // %LOCALAPPDATA%\ms-playwright on Windows, ~/.cache/ms-playwright on
        // Linux/macOS. PLAYWRIGHT_BROWSERS_PATH overrides both when set.
        var overridePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            yield return overridePath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "ms-playwright");
        }

        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(home))
        {
            yield return Path.Combine(home, ".cache", "ms-playwright");
        }
    }
}
