// Shared E2E collection (ADR-007, issue #2 follow-up): one
// EphemeralEnvironment (ephemeral Postgres with migrations applied) per
// collection run, shared by every E2E test class. E2EWebFactory reads
// EphemeralEnvironment.SharedConnectionString at host-build time
// (ConfigureWebHost runs lazily on first CreateClient, after collection
// fixtures initialize), so all classes test against the same migrated
// database. Dynamic Testcontainers ports only — never hardcoded. No real
// payments/SMS/external services are ever required.

namespace ContentFlow.E2E;

/// <summary>
/// Collection definition sharing one ephemeral Postgres, one Web host
/// factory, and one lazily-launched Chromium across the E2E suite.
/// </summary>
[CollectionDefinition(CollectionName)]
public sealed class E2ECollection : ICollectionFixture<EphemeralEnvironment>, ICollectionFixture<E2EWebFactory>, ICollectionFixture<BrowserFixture>
{
    public const string CollectionName = "E2E";
}
