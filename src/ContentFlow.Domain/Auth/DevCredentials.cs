namespace ContentFlow.Domain.Auth;

/// <summary>
/// Domain policy for the development-only admin account (ADR-004).
/// The dev admin (admin/admin) is permitted ONLY when the host runs in the Development environment.
/// Web and Migrator tracks MUST call <see cref="IsAllowed(string?)"/> with
/// <c>env.EnvironmentName</c> before seeding or enabling it; it must never be enabled in Production.
/// <para>
/// NOTE: The gate takes the environment name (rather than <c>IHostEnvironment</c>) so that
/// Domain keeps zero outward dependencies and no hosting package reference is required.
/// <c>env.IsDevelopment()</c> performs this same ordinal comparison against "Development".
/// </para>
/// </summary>
public static class DevCredentials
{
    /// <summary>The only environment name in which the dev admin account is allowed.</summary>
    public const string DevelopmentEnvironmentName = "Development";

    /// <summary>
    /// Determines whether the development-only admin account is allowed for the given environment name.
    /// Pass <c>env.EnvironmentName</c> from the host environment.
    /// </summary>
    /// <param name="environmentName">The host environment name (e.g. <c>env.EnvironmentName</c>).</param>
    /// <returns><see langword="true"/> only when <paramref name="environmentName"/> is "Development" (ordinal).</returns>
    public static bool IsAllowed(string? environmentName) =>
        string.Equals(environmentName, DevelopmentEnvironmentName, StringComparison.Ordinal);
}
