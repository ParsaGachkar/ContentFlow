using ContentFlow.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ContentItem"/>.
/// </summary>
public sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => new { x.ContentTypeId, x.Slug }).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.FieldValues)
            .WithOne(x => x.ContentItem)
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
