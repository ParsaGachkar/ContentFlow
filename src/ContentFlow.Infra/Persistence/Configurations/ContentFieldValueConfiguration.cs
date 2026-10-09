using ContentFlow.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ContentFieldValue"/>.
/// </summary>
public sealed class ContentFieldValueConfiguration : IEntityTypeConfiguration<ContentFieldValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContentFieldValue> builder)
    {
        builder.ToTable("content_field_values");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ContentItemId, x.FieldDefinitionId, x.SortOrder }).IsUnique();
        builder.HasIndex(x => x.FieldDefinitionId);
    }
}
