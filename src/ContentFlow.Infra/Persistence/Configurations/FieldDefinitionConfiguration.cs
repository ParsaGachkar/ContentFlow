using ContentFlow.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentFlow.Infra.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="FieldDefinition"/>.
/// </summary>
public sealed class FieldDefinitionConfiguration : IEntityTypeConfiguration<FieldDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FieldDefinition> builder)
    {
        builder.ToTable("field_definitions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DefaultValue).HasMaxLength(4000);

        builder.HasIndex(x => new { x.ContentTypeId, x.Key }).IsUnique();

        builder.HasMany(x => x.Values)
            .WithOne(x => x.FieldDefinition)
            .HasForeignKey(x => x.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
