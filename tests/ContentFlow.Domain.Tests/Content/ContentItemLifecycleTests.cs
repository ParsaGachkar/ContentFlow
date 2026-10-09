// Content-item lifecycle tests (issue #7, ADR-007).
//
// Covers ContentFlow.Domain.Content.ContentItem: draft start state, slug rules, and
// all four transitions (Publish Draft->Published, Unpublish Published->Draft,
// Archive Published->Archived, Rework Archived->Draft) plus every invalid transition,
// which must fail with content.status_transition. PublishedAtUtc is set on publish
// and retained as history across unpublish/archive.

using ContentFlow.Domain.Content;

namespace ContentFlow.Domain.Tests.Content;

public sealed class ContentItemLifecycleTests
{
    private static readonly Guid TypeId = Guid.NewGuid();

    private static ContentItem NewItem(string slug = "hello-world") => new(TypeId, slug);

    [Fact]
    public void NewItem_StartsAsDraft()
    {
        var item = NewItem();

        Assert.Equal(ContentStatus.Draft, item.Status);
        Assert.Equal("hello-world", item.Slug);
        Assert.Null(item.PublishedAtUtc);
    }

    [Theory]
    [InlineData("My-Post", "my-post")]
    [InlineData("  Padded  ", "padded")]
    public void Slug_IsNormalized(string raw, string expected)
    {
        Assert.Equal(expected, NewItem(raw).Slug);
    }

    [Fact]
    public void EmptyContentTypeId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ContentItem(Guid.Empty, "hello"));
    }

    [Theory]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("a--b")]
    [InlineData("has space")]
    [InlineData("under_score")]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidSlug_ThrowsArgumentException(string slug)
    {
        Assert.Throws<ArgumentException>(() => NewItem(slug));
    }

    [Fact]
    public void Publish_Draft_SucceedsAndRecordsPublishedAt()
    {
        var item = NewItem();

        var result = item.Publish();

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Published, item.Status);
        Assert.NotNull(item.PublishedAtUtc);
    }

    [Fact]
    public void Publish_WhenPublished_FailsWithStatusTransition()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);

        var result = item.Publish();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        Assert.Equal(ContentStatus.Published, item.Status);
    }

    [Fact]
    public void Publish_WhenArchived_FailsWithStatusTransition()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);
        Assert.True(item.Archive().IsSuccess);

        var result = item.Publish();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        Assert.Equal(ContentStatus.Archived, item.Status);
    }

    [Fact]
    public void Unpublish_Published_SucceedsAndRetainsPublishedAt()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);
        var publishedAt = item.PublishedAtUtc;

        var result = item.Unpublish();

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Draft, item.Status);
        Assert.Equal(publishedAt, item.PublishedAtUtc);
    }

    [Fact]
    public void Unpublish_Draft_FailsWithStatusTransition()
    {
        var item = NewItem();

        var result = item.Unpublish();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        Assert.Equal(ContentStatus.Draft, item.Status);
    }

    [Fact]
    public void Unpublish_Archived_FailsWithStatusTransition()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);
        Assert.True(item.Archive().IsSuccess);

        var result = item.Unpublish();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        Assert.Equal(ContentStatus.Archived, item.Status);
    }

    [Fact]
    public void Archive_Published_Succeeds()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);

        var result = item.Archive();

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Archived, item.Status);
        Assert.NotNull(item.PublishedAtUtc);
    }

    [Fact]
    public void Archive_Draft_FailsWithStatusTransition()
    {
        var item = NewItem();

        var result = item.Archive();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        Assert.Equal(ContentStatus.Draft, item.Status);
    }

    [Fact]
    public void Archive_Archived_FailsWithStatusTransition()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);
        Assert.True(item.Archive().IsSuccess);

        var result = item.Archive();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
    }

    [Fact]
    public void Rework_Archived_SucceedsBackToDraft()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);
        Assert.True(item.Archive().IsSuccess);

        var result = item.Rework();

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Draft, item.Status);
    }

    [Fact]
    public void Rework_Draft_FailsWithStatusTransition()
    {
        var item = NewItem();

        var result = item.Rework();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
    }

    [Fact]
    public void Rework_Published_FailsWithStatusTransition()
    {
        var item = NewItem();
        Assert.True(item.Publish().IsSuccess);

        var result = item.Rework();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        Assert.Equal(ContentStatus.Published, item.Status);
    }

    [Fact]
    public void FullCycle_Draft_Published_Archived_Draft_Published()
    {
        var item = NewItem();

        Assert.True(item.Publish().IsSuccess);
        Assert.True(item.Archive().IsSuccess);
        Assert.True(item.Rework().IsSuccess);
        Assert.Equal(ContentStatus.Draft, item.Status);
        Assert.True(item.Publish().IsSuccess);

        Assert.Equal(ContentStatus.Published, item.Status);
        Assert.NotNull(item.PublishedAtUtc);
    }
}
