// ApiKey domain tests (ADR-004 auth foundation, issue #6).
//
// Covers ContentFlow.Domain.Auth.ApiKey: IsUsableAt validity (valid / expired /
// revoked), Revoke, constructor guard clauses, and Name trimming.
// Written against the real Domain track shapes (Role/Permission/RolePermission/
// ApiKey/PermissionCodes in ContentFlow.Domain.Auth); IsUsableAt uses strict `>`
// for both revocation and expiry (boundary `== now` is NOT usable).

using ContentFlow.Domain.Auth;

namespace ContentFlow.Domain.Tests;

public sealed class ApiKeyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private static ApiKey CreateValid(
        string[]? scopes = null,
        DateTimeOffset? expiresAtUtc = null) =>
        new(
            keyPrefix: "abcd1234",
            keyHash: new string('a', 64),
            name: "test-key",
            scopes: scopes ?? ["content.read", "admin.access"],
            createdAtUtc: Now,
            expiresAtUtc: expiresAtUtc);

    [Fact]
    public void ValidKey_IsUsable()
    {
        var key = CreateValid();

        Assert.True(key.IsUsableAt(Now));
    }

    [Fact]
    public void KeyExpiringInFuture_IsUsable()
    {
        var key = CreateValid(expiresAtUtc: Now.AddHours(1));

        Assert.True(key.IsUsableAt(Now));
    }

    [Fact]
    public void ExpiredKey_IsNotUsable()
    {
        var key = CreateValid(expiresAtUtc: Now.AddHours(-1));

        Assert.False(key.IsUsableAt(Now));
    }

    [Fact]
    public void ExpiryExactlyNow_IsNotUsable()
    {
        // Boundary: IsUsableAt requires ExpiresAtUtc > now (strict).
        var key = CreateValid(expiresAtUtc: Now);

        Assert.False(key.IsUsableAt(Now));
    }

    [Fact]
    public void RevokedKey_IsNotUsable()
    {
        var key = CreateValid();
        key.Revoke(Now);

        Assert.Equal(Now, key.RevokedAtUtc);
        Assert.False(key.IsUsableAt(Now));
    }

    [Fact]
    public void KeyUsableBeforeRevocation_NotUsableAfter()
    {
        var key = CreateValid();
        var revokedAt = Now.AddHours(1);
        key.Revoke(revokedAt);

        Assert.True(key.IsUsableAt(Now));
        Assert.False(key.IsUsableAt(revokedAt));
        Assert.False(key.IsUsableAt(revokedAt.AddHours(1)));
    }

    // NOTE: the constructor uses ArgumentException.ThrowIfNullOrWhiteSpace, which throws
    // ArgumentNullException for null and ArgumentException for empty/whitespace (.NET 8+).
    // xUnit Assert.Throws requires the exact type, so null is asserted separately.
    [Fact]
    public void Constructor_RejectsNullKeyPrefix()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiKey(
            null!,
            new string('a', 64),
            "test-key",
            ["content.read"],
            Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBadKeyPrefix(string keyPrefix)
    {
        Assert.Throws<ArgumentException>(() => new ApiKey(
            keyPrefix!,
            new string('a', 64),
            "test-key",
            ["content.read"],
            Now));
    }

    [Fact]
    public void Constructor_RejectsNullKeyHash()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiKey(
            "abcd1234",
            null!,
            "test-key",
            ["content.read"],
            Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBadKeyHash(string keyHash)
    {
        Assert.Throws<ArgumentException>(() => new ApiKey(
            "abcd1234",
            keyHash!,
            "test-key",
            ["content.read"],
            Now));
    }

    [Fact]
    public void Constructor_RejectsNullName()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiKey(
            "abcd1234",
            new string('a', 64),
            null!,
            ["content.read"],
            Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBadName(string name)
    {
        Assert.Throws<ArgumentException>(() => new ApiKey(
            "abcd1234",
            new string('a', 64),
            name!,
            ["content.read"],
            Now));
    }

    [Fact]
    public void Constructor_RejectsNullScopes()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiKey(
            "abcd1234",
            new string('a', 64),
            "test-key",
            null!,
            Now));
    }

    [Fact]
    public void Constructor_RejectsEmptyScopes()
    {
        Assert.Throws<ArgumentException>(() => new ApiKey(
            "abcd1234",
            new string('a', 64),
            "test-key",
            [],
            Now));
    }

    [Fact]
    public void Constructor_RejectsBlankScopes()
    {
        Assert.Throws<ArgumentException>(() => new ApiKey(
            "abcd1234",
            new string('a', 64),
            "test-key",
            ["   "],
            Now));
    }

    [Fact]
    public void Constructor_TrimsScopes()
    {
        var key = CreateValid(scopes: ["  content.read  "]);

        Assert.Equal(["content.read"], key.Scopes);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var key = new ApiKey(
            "abcd1234",
            new string('a', 64),
            "  padded-name  ",
            ["content.read"],
            Now);

        Assert.Equal("padded-name", key.Name);
    }
}



