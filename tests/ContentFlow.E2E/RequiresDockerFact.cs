// Discovery-time gate for Docker-backed E2E tests (ADR-007).
//
// Sets FactAttribute.Skip when no Docker endpoint is present, so the runner
// reports a genuine Skipped (not Failed, not a silent pass). See
// E2EPrerequisites for why discovery-time gating is used with xUnit 2.9.3.

namespace ContentFlow.E2E;

/// <summary>
/// Marks a test as requiring a Docker endpoint (Testcontainers). Skipped with
/// a clear reason when Docker is unavailable.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!E2EPrerequisites.IsDockerEndpointPresent())
        {
            Skip = E2EPrerequisites.DockerMissingReason;
        }
    }
}
