// Shared failure type for E2E environmental problems that must go RED.
//
// xUnit 2.9.3 cannot report runtime skips (see E2EPrerequisites), so when a
// gated test DOES run but its environment collapses mid-flight (Docker dies
// after discovery, browser launch fails despite an installed build), throw
// this with an actionable message. A failure of this type always means
// "fix the environment", never "product bug".

namespace ContentFlow.E2E;

/// <summary>
/// Thrown when an E2E prerequisite fails at runtime after discovery-time
/// gating passed. Message must name the prerequisite and the remediation.
/// </summary>
public sealed class E2EEnvironmentException : Exception
{
    public E2EEnvironmentException(string message)
        : base(message)
    {
    }

    public E2EEnvironmentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
