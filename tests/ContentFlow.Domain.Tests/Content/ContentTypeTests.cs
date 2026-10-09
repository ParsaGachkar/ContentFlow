// Content-type domain tests (issue #7, ADR-007: domain invariants live in Domain.Tests).
//
// Covers ContentFlow.Domain.Content.ContentType: slug rules (valid / normalized /
// invalid), name guard, AddField duplicate-key rejection (exact + case-insensitive),
// foreign-type rejection, and the null guard. Written against the Wave-1 shapes
// (ctor ContentType(name, slug, description?), AddField(FieldDefinition) -> Result).

using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Tests.Content;

public sealed class ContentTypeTests
{
    [Theory]
    [InlineData("articles")]
    [InlineData("a")]
    [InlineData("0")]
    [InlineData("my-article")]
    [InlineData("a1-b2-3c")]
    public void ValidSlug_IsAccepted(string slug)
    {
        var type = new ContentType("Articles", slug);

        Assert.Equal(slug, type.Slug);
        Assert.Equal("Articles", type.Name);
    }

    [Theory]
    [InlineData("My-Article", "my-article")]
    [InlineData("A1", "a1")]
    [InlineData("  Padded  ", "padded")]
    public void Slug_IsNormalized_LowercaseAndTrimmed(string raw, string expected)
    {
        var type = new ContentType("T", raw);

        Assert.Equal(expected, type.Slug);
    }

    [Theory]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--hyphen")]
    [InlineData("has space")]
    [InlineData("under_score")]
    [InlineData("dot.name")]
    [InlineData("slash/name")]
    [InlineData("café")]
    [InlineData("a!b")]
    public void InvalidSlug_ThrowsArgumentException(string slug)
    {
        Assert.Throws<ArgumentException>(() => new ContentType("T", slug));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSlug_ThrowsArgumentException(string slug)
    {
        Assert.Throws<ArgumentException>(() => new ContentType("T", slug));
    }

    [Fact]
    public void NullSlug_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ContentType("T", null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new ContentType(name!, "valid-slug"));
    }

    [Fact]
    public void Name_IsTrimmed()
    {
        var type = new ContentType("  News  ", "news");

        Assert.Equal("News", type.Name);
    }

    private static ContentType NewType() => new("Articles", "articles");

    private static FieldDefinition NewField(Guid typeId, string key) =>
        new(typeId, "Title", key, FieldDataType.Text);

    [Fact]
    public void AddField_ValidField_Succeeds()
    {
        var type = NewType();

        var result = type.AddField(NewField(type.Id, "title"));

        Assert.True(result.IsSuccess);
        Assert.Single(type.Fields);
    }

    [Fact]
    public void AddField_DistinctKeys_AllSucceed()
    {
        var type = NewType();

        Assert.True(type.AddField(NewField(type.Id, "title")).IsSuccess);
        Assert.True(type.AddField(NewField(type.Id, "subtitle")).IsSuccess);

        Assert.Equal(2, type.Fields.Count);
    }

    [Fact]
    public void AddField_DuplicateKey_FailsWithDuplicateKey()
    {
        var type = NewType();
        Assert.True(type.AddField(NewField(type.Id, "title")).IsSuccess);

        var result = type.AddField(NewField(type.Id, "title"));

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.DuplicateKey, result.Error!.Code);
        Assert.Single(type.Fields);
    }

    [Fact]
    public void AddField_DuplicateKey_IsCaseInsensitive()
    {
        var type = NewType();
        Assert.True(type.AddField(NewField(type.Id, "Title")).IsSuccess);

        var result = type.AddField(NewField(type.Id, "TITLE"));

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.DuplicateKey, result.Error!.Code);
        Assert.Single(type.Fields);
    }

    [Fact]
    public void AddField_ForeignTypeField_FailsWithTypeMismatch()
    {
        var type = NewType();
        var foreign = NewField(Guid.NewGuid(), "title");

        var result = type.AddField(foreign);

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.TypeMismatch, result.Error!.Code);
        Assert.Empty(type.Fields);
    }

    [Fact]
    public void AddField_Null_ThrowsArgumentNullException()
    {
        var type = NewType();

        Assert.Throws<ArgumentNullException>(() => type.AddField(null!));
    }
}

