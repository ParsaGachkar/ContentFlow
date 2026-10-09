// Permission-checker contract tests (ADR-004 auth foundation, issue #6).
//
// DRIFT NOTE (read before extending): at the time of writing, src contains ONLY the
// IPermissionChecker abstraction (ContentFlow.Application.Shared.Authorization:
// Task<bool> HasAsync(ClaimsPrincipal, string, CancellationToken)) — the Application
// track has not landed a concrete checker with store dependencies yet, so there is no
// implementation to drive. These tests therefore pin the contract every server-side
// consumer must enforce through (allow path / deny path / per-code isolation), plus a
// reflection guard on the exact HasAsync signature. They MUST be extended with
// implementation allow/deny tests (NSubstitute for stores) when the checker lands —
// they are not a substitute for those.

using System.Reflection;
using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Domain.Auth;
using NSubstitute;

namespace ContentFlow.Application.Tests;

public sealed class PermissionCheckerTests
{
    private static ClaimsPrincipal TestPrincipal() =>
        new(new ClaimsIdentity("TestAuth"));

    [Fact]
    public async Task AllowPath_CheckerGrantingPermission_ReturnsTrue()
    {
        var checker = Substitute.For<IPermissionChecker>();
        var principal = TestPrincipal();
        checker
            .HasAsync(principal, PermissionCodes.AdminAccess, Arg.Any<CancellationToken>())
            .Returns(true);

        var granted = await checker.HasAsync(principal, PermissionCodes.AdminAccess, CancellationToken.None);

        Assert.True(granted);
    }

    [Fact]
    public async Task DenyPath_CheckerWithoutGrant_ReturnsFalse()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker
            .HasAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var granted = await checker.HasAsync(TestPrincipal(), PermissionCodes.AdminAccess, CancellationToken.None);

        Assert.False(granted);
    }

    [Fact]
    public async Task DistinctCodes_AreEvaluatedIndependently()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker
            .HasAsync(Arg.Any<ClaimsPrincipal>(), PermissionCodes.AdminAccess, Arg.Any<CancellationToken>())
            .Returns(true);
        checker
            .HasAsync(Arg.Any<ClaimsPrincipal>(), PermissionCodes.ContentWrite, Arg.Any<CancellationToken>())
            .Returns(false);

        Assert.True(await checker.HasAsync(TestPrincipal(), PermissionCodes.AdminAccess, CancellationToken.None));
        Assert.False(await checker.HasAsync(TestPrincipal(), PermissionCodes.ContentWrite, CancellationToken.None));
    }

    [Fact]
    public void AdminAccessCode_MatchesApiContractScope()
    {
        // GET /api/v1/admin/status requires this exact scope (ApiKey scheme).
        Assert.Equal("admin.access", PermissionCodes.AdminAccess);
    }

    [Fact]
    public void ContractShape_HasAsyncSignature_IsStable()
    {
        var method = typeof(IPermissionChecker).GetMethod(
            nameof(IPermissionChecker.HasAsync),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<bool>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(ClaimsPrincipal), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }
}
