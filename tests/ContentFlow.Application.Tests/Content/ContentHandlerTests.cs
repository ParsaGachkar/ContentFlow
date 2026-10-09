// Content handler tests (issue #7, ADR-007: application use cases with mocked
// abstractions via NSubstitute).
//
// Direct tests over the real Application-track handlers
// (ContentFlow.Application.Content.*Handler, HandleAsync(cmd, ClaimsPrincipal, ct)
// -> Result<TDto>), which match the specified contract shape. Repositories,
// IPermissionChecker, and IValidator<T> are NSubstitute mocks, except where a REAL
// FluentValidation validator is used to prove end-to-end validation short-circuiting.
// Companion file ContentUseCaseTests pins the orchestration contract at seam level;
// this file proves the handlers implement it (permission gate before mutation,
// validation before permission check, persist-only-on-success).
//
// Verified handler facts these tests rely on:
// - Pipeline order: FluentValidation -> permission check -> load/dedup -> domain ->
//   persist. Denials/failures return before any Add/SaveChangesAsync.
// - Error codes: content.validation (validator + domain ArgumentException fold),
//   content.forbidden, content.not_found, content.duplicate_slug,
//   plus domain codes (content.status_transition, content.field_unknown, ...).
// - UpdateContentItemFieldsHandler rejects non-draft items with
//   content.status_transition BEFORE touching navigations, so a hand-built published
//   item (null ContentType nav, as returned for a bare mock) exercises the guard.
//   The draft-update happy path needs EF-loaded navigations and is covered by the
//   Infra round-trip + domain upsert tests instead.

using System.Security.Claims;
using ContentFlow.Application.Content;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;

namespace ContentFlow.Application.Tests.Content;

public sealed class ContentHandlerTests
{
    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity("TestAuth"));

    private static IPermissionChecker GrantingChecker(string code)
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker
            .HasAsync(Arg.Any<ClaimsPrincipal>(), code, Arg.Any<CancellationToken>())
            .Returns(true);
        return checker;
    }

    private static IPermissionChecker DenyingChecker()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker
            .HasAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        return checker;
    }

    private static IValidator<T> PassingValidator<T>()
    {
        var validator = Substitute.For<IValidator<T>>();
        validator
            .ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        return validator;
    }

    private static IValidator<T> FailingValidator<T>(params string[] messages)
    {
        var validator = Substitute.For<IValidator<T>>();
        validator
            .ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(messages.Select(m => new ValidationFailure("Field", m))));
        return validator;
    }

    // --- CreateContentTypeHandler ---

    [Fact]
    public async Task CreateType_Forbidden_ReturnsForbiddenWithoutMutation()
    {
        var types = Substitute.For<IContentTypeRepository>();
        var handler = new CreateContentTypeHandler(types, DenyingChecker(), PassingValidator<CreateContentTypeCommand>());

        var result = await handler.HandleAsync(new CreateContentTypeCommand("Articles", "articles"), Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.forbidden", result.Error!.Code);
        await types.DidNotReceive().SlugExistsAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>());
        types.DidNotReceive().Add(Arg.Any<ContentType>());
        await types.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateType_ValidationFailure_ShortCircuitsBeforePermissionCheck()
    {
        var types = Substitute.For<IContentTypeRepository>();
        var permissions = Substitute.For<IPermissionChecker>();
        var handler = new CreateContentTypeHandler(types, permissions, FailingValidator<CreateContentTypeCommand>("bad"));

        var result = await handler.HandleAsync(new CreateContentTypeCommand("", "bad slug!!"), Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.validation", result.Error!.Code);
        await permissions.DidNotReceive().HasAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        types.DidNotReceive().Add(Arg.Any<ContentType>());
    }

    [Fact]
    public async Task CreateType_RealValidator_InvalidSlug_FailsWithoutPermissionCheck()
    {
        var types = Substitute.For<IContentTypeRepository>();
        var permissions = Substitute.For<IPermissionChecker>();
        var handler = new CreateContentTypeHandler(types, permissions, new CreateContentTypeValidator());

        var result = await handler.HandleAsync(new CreateContentTypeCommand("Articles", "bad slug!!"), Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.validation", result.Error!.Code);
        await permissions.DidNotReceive().HasAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateType_DuplicateSlug_FailsWithoutMutation()
    {
        var types = Substitute.For<IContentTypeRepository>();
        types
            .SlugExistsAsync("articles", null, Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new CreateContentTypeHandler(
            types, GrantingChecker(PermissionCodes.ContentWrite), PassingValidator<CreateContentTypeCommand>());

        var result = await handler.HandleAsync(new CreateContentTypeCommand("Articles", "articles"), Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.duplicate_slug", result.Error!.Code);
        types.DidNotReceive().Add(Arg.Any<ContentType>());
        await types.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateType_HappyPath_PersistsAndReturnsDto()
    {
        var types = Substitute.For<IContentTypeRepository>();
        types
            .SlugExistsAsync("articles", null, Arg.Any<CancellationToken>())
            .Returns(false);
        types.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new CreateContentTypeHandler(
            types, GrantingChecker(PermissionCodes.ContentWrite), PassingValidator<CreateContentTypeCommand>());

        var result = await handler.HandleAsync(
            new CreateContentTypeCommand("Articles", "articles", "desc"), Principal());

        Assert.True(result.IsSuccess);
        Assert.Equal("articles", result.Value!.Slug);
        Assert.Equal("Articles", result.Value.Name);
        types.Received(1).Add(Arg.Is<ContentType>(t => t.Slug == "articles"));
        await types.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // --- Publish / Unpublish lifecycle handlers ---

    [Fact]
    public async Task Publish_Forbidden_ReturnsForbiddenWithoutLoading()
    {
        var items = Substitute.For<IContentItemRepository>();
        var handler = new PublishContentItemHandler(items, DenyingChecker(), PassingValidator<PublishContentItemCommand>());

        var result = await handler.HandleAsync(new PublishContentItemCommand(Guid.NewGuid()), Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.forbidden", result.Error!.Code);
        await items.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_MissingItem_ReturnsNotFound()
    {
        var items = Substitute.For<IContentItemRepository>();
        items.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ContentItem?)null);
        var handler = new PublishContentItemHandler(
            items, GrantingChecker(PermissionCodes.ContentPublish), PassingValidator<PublishContentItemCommand>());

        var result = await handler.HandleAsync(new PublishContentItemCommand(Guid.NewGuid()), Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.not_found", result.Error!.Code);
        await items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_HappyPath_TransitionsAndSaves()
    {
        var items = Substitute.For<IContentItemRepository>();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");
        items.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        items.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new PublishContentItemHandler(
            items, GrantingChecker(PermissionCodes.ContentPublish), PassingValidator<PublishContentItemCommand>());

        var result = await handler.HandleAsync(new PublishContentItemCommand(item.Id), Principal());

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Published, result.Value!.Status);
        Assert.NotNull(result.Value.PublishedAtUtc);
        await items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_WhenPublished_FailsWithStatusTransitionWithoutSaving()
    {
        var items = Substitute.For<IContentItemRepository>();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");
        Assert.True(item.Publish().IsSuccess);
        items.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new PublishContentItemHandler(
            items, GrantingChecker(PermissionCodes.ContentPublish), PassingValidator<PublishContentItemCommand>());

        var result = await handler.HandleAsync(new PublishContentItemCommand(item.Id), Principal());

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        await items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unpublish_HappyPath_TransitionsAndRetainsPublishedAt()
    {
        var items = Substitute.For<IContentItemRepository>();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");
        Assert.True(item.Publish().IsSuccess);
        var publishedAt = item.PublishedAtUtc;
        items.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        items.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new UnpublishContentItemHandler(
            items, GrantingChecker(PermissionCodes.ContentPublish), PassingValidator<UnpublishContentItemCommand>());

        var result = await handler.HandleAsync(new UnpublishContentItemCommand(item.Id), Principal());

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Draft, result.Value!.Status);
        Assert.Equal(publishedAt, result.Value.PublishedAtUtc);
        await items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // --- UpdateContentItemFieldsHandler ---

    [Fact]
    public async Task Update_Forbidden_ReturnsForbiddenWithoutLoading()
    {
        var items = Substitute.For<IContentItemRepository>();
        var handler = new UpdateContentItemFieldsHandler(
            items, DenyingChecker(), PassingValidator<UpdateContentItemFieldsCommand>());

        var result = await handler.HandleAsync(
            new UpdateContentItemFieldsCommand(Guid.NewGuid(), new Dictionary<string, string?> { ["title"] = "x" }),
            Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.forbidden", result.Error!.Code);
        await items.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_PublishedItem_FailsWithStatusTransitionWithoutSaving()
    {
        var items = Substitute.For<IContentItemRepository>();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");
        Assert.True(item.Publish().IsSuccess);
        items.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new UpdateContentItemFieldsHandler(
            items, GrantingChecker(PermissionCodes.ContentWrite), PassingValidator<UpdateContentItemFieldsCommand>());

        var result = await handler.HandleAsync(
            new UpdateContentItemFieldsCommand(item.Id, new Dictionary<string, string?> { ["title"] = "edited" }),
            Principal());

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        await items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_MissingItem_ReturnsNotFound()
    {
        var items = Substitute.For<IContentItemRepository>();
        items.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ContentItem?)null);
        var handler = new UpdateContentItemFieldsHandler(
            items, GrantingChecker(PermissionCodes.ContentWrite), PassingValidator<UpdateContentItemFieldsCommand>());

        var result = await handler.HandleAsync(
            new UpdateContentItemFieldsCommand(Guid.NewGuid(), new Dictionary<string, string?> { ["title"] = "x" }),
            Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.not_found", result.Error!.Code);
    }

    // --- CreateContentItemHandler ---

    private static ContentType TypeWithTitleField()
    {
        var type = new ContentType("Articles", "articles");
        Assert.True(type.AddField(new FieldDefinition(type.Id, "Title", "title", FieldDataType.Text)).IsSuccess);
        return type;
    }

    [Fact]
    public async Task CreateItem_Forbidden_ReturnsForbiddenWithoutLoading()
    {
        var types = Substitute.For<IContentTypeRepository>();
        var items = Substitute.For<IContentItemRepository>();
        var handler = new CreateContentItemHandler(
            types, items, DenyingChecker(), PassingValidator<CreateContentItemCommand>());

        var result = await handler.HandleAsync(
            new CreateContentItemCommand(Guid.NewGuid(), "hello", new Dictionary<string, string?>()),
            Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.forbidden", result.Error!.Code);
        await types.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        items.DidNotReceive().Add(Arg.Any<ContentItem>());
    }

    [Fact]
    public async Task CreateItem_HappyPath_PersistsDraftWithValues()
    {
        var type = TypeWithTitleField();
        var types = Substitute.For<IContentTypeRepository>();
        types.GetByIdAsync(type.Id, Arg.Any<CancellationToken>()).Returns(type);
        var items = Substitute.For<IContentItemRepository>();
        items
            .SlugExistsAsync(type.Id, "hello-world", null, Arg.Any<CancellationToken>())
            .Returns(false);
        items.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(2);
        var handler = new CreateContentItemHandler(
            types, items, GrantingChecker(PermissionCodes.ContentWrite), PassingValidator<CreateContentItemCommand>());

        var result = await handler.HandleAsync(
            new CreateContentItemCommand(
                type.Id, "hello-world", new Dictionary<string, string?> { ["title"] = "Hello" }),
            Principal());

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Draft, result.Value!.Status);
        Assert.Equal("Hello", result.Value.Values["title"]);
        items.Received(1).Add(Arg.Is<ContentItem>(i => i.Slug == "hello-world"));
        await items.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateItem_DuplicateSlug_FailsWithoutMutation()
    {
        var type = TypeWithTitleField();
        var types = Substitute.For<IContentTypeRepository>();
        types.GetByIdAsync(type.Id, Arg.Any<CancellationToken>()).Returns(type);
        var items = Substitute.For<IContentItemRepository>();
        items
            .SlugExistsAsync(type.Id, "hello-world", null, Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new CreateContentItemHandler(
            types, items, GrantingChecker(PermissionCodes.ContentWrite), PassingValidator<CreateContentItemCommand>());

        var result = await handler.HandleAsync(
            new CreateContentItemCommand(
                type.Id, "hello-world", new Dictionary<string, string?> { ["title"] = "Hello" }),
            Principal());

        Assert.True(result.IsFailure);
        Assert.Equal("content.duplicate_slug", result.Error!.Code);
        items.DidNotReceive().Add(Arg.Any<ContentItem>());
    }

    [Fact]
    public async Task CreateItem_UnknownFieldKey_FailsWithoutMutation()
    {
        var type = TypeWithTitleField();
        var types = Substitute.For<IContentTypeRepository>();
        types.GetByIdAsync(type.Id, Arg.Any<CancellationToken>()).Returns(type);
        var items = Substitute.For<IContentItemRepository>();
        items
            .SlugExistsAsync(type.Id, "hello-world", null, Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new CreateContentItemHandler(
            types, items, GrantingChecker(PermissionCodes.ContentWrite), PassingValidator<CreateContentItemCommand>());

        var result = await handler.HandleAsync(
            new CreateContentItemCommand(
                type.Id, "hello-world", new Dictionary<string, string?> { ["nope"] = "x" }),
            Principal());

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.FieldUnknown, result.Error!.Code);
        items.DidNotReceive().Add(Arg.Any<ContentItem>());
        await items.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
