using ContentFlow.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ContentType"/>.
/// </summary>
public sealed class ContentTypeConfiguration : IEntityTypeConfiguration<ContentType>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContentType> builder)
    {
        builder.ToTable("content_types");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasMany(x => x.Fields)
            .WithOne(x => x.ContentType)
            .HasForeignKey(x => x.ContentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.ContentType)
            .HasForeignKey(x => x.ContentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
