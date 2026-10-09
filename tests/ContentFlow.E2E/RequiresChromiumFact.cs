// Discovery-time gate for Playwright browser E2E tests (ADR-007).
//
// Sets FactAttribute.Skip when no Playwright Chromium build is installed, so
// the runner reports a genuine Skipped (not Failed, not a silent pass). See
// E2EPrerequisites for why discovery-time gating is used with xUnit 2.9.3.

namespace ContentFlow.E2E;

/// <summary>
/// Marks a test as requiring an installed Playwright Chromium browser.
/// Skipped with install guidance when browsers are unavailable.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresChromiumFactAttribute : FactAttribute
{
    public RequiresChromiumFactAttribute()
    {
        if (!E2EPrerequisites.IsChromiumPresent())
        {
            Skip = E2EPrerequisites.ChromiumMissingReason;
        }
    }
}
