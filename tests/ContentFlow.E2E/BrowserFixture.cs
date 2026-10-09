// Playwright Chromium for E2E (ADR-007). Owned by the E2E suite; launches
// one headless Chromium per fixture, lazily on first page request so that
// HTTP-only test runs never pay for (or require) a browser.
//
// Browser install (once per machine, NOT part of test runs):
//   dotnet build tests/ContentFlow.E2E/ContentFlow.E2E.csproj
//   pwsh tests/ContentFlow.E2E/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
// The Microsoft.Playwright package emits playwright.ps1 into the build output
// on build; --with-deps installs OS-level browser dependencies on Linux.
// Alternatively set PLAYWRIGHT_BROWSERS_PATH to a shared browser location.
//
// Graceful degradation: discovery-time gating ([RequiresChromiumFact]) skips
// browser tests when no Chromium build is installed. If a launch fails at
// runtime despite an installed build, NewPageAsync throws
// E2EEnvironmentException (red, with remediation) — a corrupt install is a
// real problem, not a skip.

using Microsoft.Playwright;

namespace ContentFlow.E2E;

/// <summary>
/// xUnit class fixture owning a lazily-launched headless Chromium browser.
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    private readonly SemaphoreSlim _launchGate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>True once Chromium launched successfully.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Why the browser is unavailable (null when available).</summary>
    public string? UnavailableReason { get; private set; }

    public Task InitializeAsync()
    {
        // Intentionally lazy: never launch (or fail) here so HTTP-only runs
        // stay browser-independent. Presence was already probed at discovery
        // by [RequiresChromiumFact]; record the fast-path reason up front.
        if (!E2EPrerequisites.IsChromiumPresent())
        {
            UnavailableReason = E2EPrerequisites.ChromiumMissingReason;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a fresh page on the shared headless Chromium, launching it on
    /// first use. Throws <see cref="E2EEnvironmentException"/> when the
    /// browser cannot launch.
    /// </summary>
    public async Task<IPage> NewPageAsync()
    {
        if (IsAvailable && _browser is not null)
        {
            return await _browser.NewPageAsync();
        }

        await _launchGate.WaitAsync();
        try
        {
            if (IsAvailable && _browser is not null)
            {
                return await _browser.NewPageAsync();
            }

            try
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    // Required for root/CI containers; harmless on dev boxes.
                    Args = ["--no-sandbox"],
                });
            }
            catch (Exception ex)
            {
                UnavailableReason =
                    "E2E prerequisite failed at runtime: Chromium could not launch " +
                    $"({ex.GetType().Name}: {ex.Message}). " +
                    "Reinstall browsers with: " +
                    "pwsh tests/ContentFlow.E2E/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium";
                throw new E2EEnvironmentException(UnavailableReason, ex);
            }

            IsAvailable = true;
            return await _browser.NewPageAsync();
        }
        finally
        {
            _launchGate.Release();
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }

        if (_playwright is not null)
        {
            _playwright.Dispose();
            _playwright = null;
        }

        _launchGate.Dispose();
        IsAvailable = false;
    }
}
