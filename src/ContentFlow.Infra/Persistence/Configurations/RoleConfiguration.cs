using ContentFlow.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Role"/>.
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.NormalizedName).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => x.NormalizedName).IsUnique();
    }
}
