using ContentFlow.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ApiKey"/>.
/// Only the SHA-256 hex digest (<see cref="ApiKey.KeyHash"/>) is stored; scopes are a
/// space-separated string column (relational-first per ADR-002, no JSON document).
/// </summary>
public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.KeyPrefix).IsRequired().HasMaxLength(32);
        builder.Property(x => x.KeyHash).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Scopes).IsRequired().HasMaxLength(2000);

        builder.HasIndex(x => x.KeyPrefix);
        builder.HasIndex(x => x.KeyHash).IsUnique();
    }
}
