using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Domain.Auth;
using ContentFlow.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentFlow.Infra.Auth;

/// <summary>
/// Development-only admin seeder (ADR-004, issue #6).
/// <para>
/// EVERYTHING is gated behind <see cref="DevCredentials.IsAllowed(string?)"/> using the
/// injected host's environment name: any call outside Development throws
/// <see cref="InvalidOperationException"/> before touching the database. There is
/// intentionally NO production path (callers ALSO gate; this is defense in depth).
/// </para>
/// <para>
/// No <c>User</c> entity exists yet, so no identity schema is invented here: the seeder
/// ensures the <c>admin</c> role plus all <see cref="PermissionCodes.All"/> permission rows
/// and their <see cref="RolePermission"/> links (idempotent ensure-pattern, safe to run on
/// every dev startup). Dev-login credential verification lives in the Web track's login
/// endpoint (constant-time compare against config); the plaintext password is never
/// stored here and never logged.
/// </para>
/// </summary>
public sealed class DevAdminSeeder : IDevAdminSeeder
{
    private const string AdminRoleName = "admin";

    private static readonly IReadOnlyDictionary<string, string> SeedPermissionDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PermissionCodes.ContentRead] = "Read published content.",
            [PermissionCodes.ContentWrite] = "Create and edit content.",
            [PermissionCodes.ContentPublish] = "Publish and unpublish content.",
            [PermissionCodes.AdminAccess] = "Access administration areas.",
            [PermissionCodes.MediaManage] = "Upload and manage media assets.",
        };

    private readonly ContentFlowDbContext _db;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevAdminSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevAdminSeeder"/> class.
    /// </summary>
    public DevAdminSeeder(ContentFlowDbContext db, IHostEnvironment environment, ILogger<DevAdminSeeder> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken ct)
    {
        if (!DevCredentials.IsAllowed(_environment.EnvironmentName))
        {
            throw new InvalidOperationException(
                "Dev admin seeding is only allowed in the Development environment.");
        }

        foreach (var code in PermissionCodes.All)
        {
            var exists = await _db.Permissions
                .AnyAsync(p => p.Code == code, ct)
                .ConfigureAwait(false);
            if (!exists)
            {
                _db.Permissions.Add(new Permission(code, SeedPermissionDescriptions[code]));
            }
        }

        var normalizedAdmin = Role.NormalizeName(AdminRoleName);
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .SingleOrDefaultAsync(r => r.NormalizedName == normalizedAdmin, ct)
            .ConfigureAwait(false);
        if (role is null)
        {
            role = new Role(AdminRoleName);
            _db.Roles.Add(role);
        }

        var permissions = await _db.Permissions
            .Where(p => PermissionCodes.All.Contains(p.Code))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var permission in permissions)
        {
            if (role.RolePermissions.All(rp => rp.PermissionId != permission.Id))
            {
                role.RolePermissions.Add(new RolePermission(role.Id, permission.Id));
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Dev admin role '{Role}' ensured with {Count} permissions.",
            AdminRoleName,
            permissions.Count);
    }
}
