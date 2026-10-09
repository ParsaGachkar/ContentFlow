using ContentFlow.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="MediaAsset"/>.
/// </summary>
public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.AltText).HasMaxLength(1000);

        builder.HasIndex(x => x.StorageKey).IsUnique();
    }
}
