// Field-definition domain tests (issue #7, ADR-007).
//
// Covers ContentFlow.Domain.Content.FieldDefinition: key normalization, key/name/type
// guards, MaxLength rules (positive + Text/LongText only), and default-value parsing
// per data type. Written against the Wave-1 ctor
// FieldDefinition(contentTypeId, name, key, dataType, isRequired, sortOrder,
// defaultValue, maxLength), which throws ArgumentException on invariant violations.

using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Tests.Content;

public sealed class FieldDefinitionTests
{
    private static readonly Guid TypeId = Guid.NewGuid();

    [Fact]
    public void Valid_SetsPropertiesWithDefaults()
    {
        var field = new FieldDefinition(TypeId, "Title", "title", FieldDataType.Text);

        Assert.Equal(TypeId, field.ContentTypeId);
        Assert.Equal("Title", field.Name);
        Assert.Equal("title", field.Key);
        Assert.Equal(FieldDataType.Text, field.DataType);
        Assert.False(field.IsRequired);
        Assert.Equal(0, field.SortOrder);
        Assert.Null(field.DefaultValue);
        Assert.Null(field.MaxLength);
    }

    [Theory]
    [InlineData("Headline", "headline")]
    [InlineData("BODY-TEXT", "body-text")]
    [InlineData("  Padded-Key  ", "padded-key")]
    public void Key_IsNormalized_LowercaseAndTrimmed(string raw, string expected)
    {
        var field = new FieldDefinition(TypeId, "Name", raw, FieldDataType.Text);

        Assert.Equal(expected, field.Key);
    }

    [Theory]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("a--b")]
    [InlineData("has space")]
    [InlineData("under_score")]
    [InlineData("dot.key")]
    public void InvalidKey_ThrowsArgumentException(string key)
    {
        Assert.Throws<ArgumentException>(() => new FieldDefinition(TypeId, "Name", key, FieldDataType.Text));
    }

    [Fact]
    public void NullKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FieldDefinition(TypeId, "Name", null!, FieldDataType.Text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new FieldDefinition(TypeId, name!, "key", FieldDataType.Text));
    }

    [Fact]
    public void EmptyContentTypeId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FieldDefinition(Guid.Empty, "Name", "key", FieldDataType.Text));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveMaxLength_ThrowsArgumentException(int maxLength)
    {
        Assert.Throws<ArgumentException>(() =>
            new FieldDefinition(TypeId, "Name", "key", FieldDataType.Text, maxLength: maxLength));
    }

    [Theory]
    [InlineData(FieldDataType.Number)]
    [InlineData(FieldDataType.Boolean)]
    [InlineData(FieldDataType.DateTime)]
    public void MaxLength_OnNonTextType_ThrowsArgumentException(FieldDataType dataType)
    {
        Assert.Throws<ArgumentException>(() =>
            new FieldDefinition(TypeId, "Name", "key", dataType, maxLength: 10));
    }

    [Theory]
    [InlineData(FieldDataType.Text)]
    [InlineData(FieldDataType.LongText)]
    public void MaxLength_OnTextTypes_Succeeds(FieldDataType dataType)
    {
        var field = new FieldDefinition(TypeId, "Name", "key", dataType, maxLength: 10);

        Assert.Equal(10, field.MaxLength);
    }

    [Theory]
    [InlineData(FieldDataType.Number, "abc", null)]
    [InlineData(FieldDataType.Boolean, "yes", null)]
    [InlineData(FieldDataType.DateTime, "not-a-date", null)]
    [InlineData(FieldDataType.Text, "toolong", 3)]
    public void InvalidDefaultValue_ThrowsArgumentException(FieldDataType dataType, string defaultValue, int? maxLength)
    {
        Assert.Throws<ArgumentException>(() =>
            new FieldDefinition(TypeId, "Name", "key", dataType, defaultValue: defaultValue, maxLength: maxLength));
    }

    [Theory]
    [InlineData(FieldDataType.Number, "3.5")]
    [InlineData(FieldDataType.Boolean, "true")]
    [InlineData(FieldDataType.DateTime, "2026-10-09T12:00:00+00:00")]
    [InlineData(FieldDataType.Text, "anything goes")]
    [InlineData(FieldDataType.LongText, "multi\nline")]
    public void ValidDefaultValue_Succeeds(FieldDataType dataType, string defaultValue)
    {
        var field = new FieldDefinition(TypeId, "Name", "key", dataType, defaultValue: defaultValue);

        Assert.Equal(defaultValue, field.DefaultValue);
    }
}

