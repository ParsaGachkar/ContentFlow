using ContentFlow.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Permission"/>.
/// </summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
