namespace ContentFlow.Application.Shared.Authorization;

/// <summary>
/// Seeds the development-only admin account. Implementation lives in Infra.
/// Callers (Web/Migrator) MUST gate execution with the <c>DevCredentials</c> domain policy
/// (allowed in the Development environment only); it must never run in Production.
/// </summary>
public interface IDevAdminSeeder
{
    /// <summary>
    /// Seeds the development-only admin account.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SeedAsync(CancellationToken ct);
}
