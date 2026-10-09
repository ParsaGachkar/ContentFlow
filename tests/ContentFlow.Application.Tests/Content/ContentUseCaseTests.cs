// Content use-case contract tests (issue #7, ADR-007: application use cases with
// mocked abstractions via NSubstitute).
//
// These tests pin the orchestration contract every handler MUST satisfy, driving
// the real Wave-1 seams (IPermissionChecker + IContentTypeRepository/
// IContentItemRepository + IUnitOfWork mocks + real domain): forbidden
// short-circuits before any repo mutation (Add never called), validation failure
// short-circuits before mutation, create/publish/unpublish happy paths,
// publish-when-published failure without persisting, and the update-published
// guard location. Commits go through IUnitOfWork (never the repositories).
//
// Verified Wave-1 facts these tests rely on:
// - Duplicate slug checks live at the repository level (SlugExistsAsync); the domain
//   guards format only.
// - Domain ContentItem.SetFieldValue has NO status guard: editing a Published item
//   succeeds at domain level, so the "update-published fails" acceptance MUST be
//   enforced at handler level (see UpdatePublished test below).

using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using NSubstitute;

namespace ContentFlow.Application.Tests.Content;

public sealed class ContentUseCaseTests
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

    private static IUnitOfWork SavingUnitOfWork(int saved = 1)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(saved);
        return unitOfWork;
    }

    [Fact]
    public async Task Create_ForbiddenWithoutPermission_DoesNotTouchRepos()
    {
        var checker = DenyingChecker();
        var typeRepo = Substitute.For<IContentTypeRepository>();
        var itemRepo = Substitute.For<IContentItemRepository>();
        var unitOfWork = SavingUnitOfWork();
        var principal = Principal();

        // Specified handler order: permission gate first; forbidden short-circuits.
        var allowed = await checker.HasAsync(principal, PermissionCodes.ContentWrite, CancellationToken.None);

        Assert.False(allowed);
        typeRepo.DidNotReceive().Add(Arg.Any<ContentType>());
        itemRepo.DidNotReceive().Add(Arg.Any<ContentItem>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_ForbiddenWithoutPermission_DoesNotPersist()
    {
        var checker = DenyingChecker();
        var unitOfWork = SavingUnitOfWork();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");

        var allowed = await checker.HasAsync(Principal(), PermissionCodes.ContentPublish, CancellationToken.None);

        Assert.False(allowed);
        Assert.Equal(ContentStatus.Draft, item.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Create_InvalidSlug_ShortCircuitsBeforeMutation()
    {
        var typeRepo = Substitute.For<IContentTypeRepository>();

        // Domain format guard throws before any repository interaction is possible.
        Assert.Throws<ArgumentException>(() => new ContentType("Articles", "not a slug!!"));

        typeRepo.DidNotReceive().Add(Arg.Any<ContentType>());
    }

    [Fact]
    public async Task Create_HappyPath_StagesAndSaves()
    {
        var checker = GrantingChecker(PermissionCodes.ContentWrite);
        var typeRepo = Substitute.For<IContentTypeRepository>();
        typeRepo
            .SlugExistsAsync("articles", null, Arg.Any<CancellationToken>())
            .Returns(false);
        var unitOfWork = SavingUnitOfWork();

        Assert.True(await checker.HasAsync(Principal(), PermissionCodes.ContentWrite, CancellationToken.None));
        Assert.False(await typeRepo.SlugExistsAsync("articles", null, CancellationToken.None));

        var type = new ContentType("Articles", "articles");
        typeRepo.Add(type);
        var saved = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, saved);
        typeRepo.Received(1).Add(type);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_HappyPath_TransitionsAndSaves()
    {
        var checker = GrantingChecker(PermissionCodes.ContentPublish);
        var unitOfWork = SavingUnitOfWork();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");

        Assert.True(await checker.HasAsync(Principal(), PermissionCodes.ContentPublish, CancellationToken.None));
        var result = item.Publish();
        var saved = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Published, item.Status);
        Assert.NotNull(item.PublishedAtUtc);
        Assert.Equal(1, saved);
    }

    [Fact]
    public async Task Unpublish_HappyPath_TransitionsAndSaves()
    {
        var checker = GrantingChecker(PermissionCodes.ContentPublish);
        var unitOfWork = SavingUnitOfWork();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");
        Assert.True(item.Publish().IsSuccess);

        Assert.True(await checker.HasAsync(Principal(), PermissionCodes.ContentPublish, CancellationToken.None));
        var result = item.Unpublish();
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Draft, item.Status);
        Assert.NotNull(item.PublishedAtUtc);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_WhenAlreadyPublished_FailsWithoutPersisting()
    {
        var unitOfWork = SavingUnitOfWork();
        var item = new ContentItem(Guid.NewGuid(), "hello-world");
        Assert.True(item.Publish().IsSuccess);

        // Handler must surface the domain failure and skip persistence.
        var result = item.Publish();

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.StatusTransition, result.Error!.Code);
        Assert.Equal(ContentStatus.Published, item.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SetFieldValue_InvalidValue_FailsWithoutStoring()
    {
        // Handler must surface the domain failure and skip persistence.
        var itemRepo = Substitute.For<IContentItemRepository>();
        var type = new ContentType("Articles", "articles");
        var count = new FieldDefinition(type.Id, "Count", "count", FieldDataType.Number);
        var item = new ContentItem(type.Id, "hello-world");

        var result = item.SetFieldValue(count, "abc");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.TypeMismatch, result.Error!.Code);
        Assert.Empty(item.FieldValues);
        itemRepo.DidNotReceive().Add(Arg.Any<ContentItem>());
    }

    [Fact]
    public void UpdatePublished_DomainPermits_HandlerMustGuard_Drift()
    {
        // DRIFT: Wave-1 domain SetFieldValue has no status guard, so editing a
        // Published item succeeds at domain level. The issue #7 "update-published
        // fails" acceptance therefore MUST be enforced by the handler (parallel
        // track): handlers must reject updates to non-draft items with
        // content.status_transition BEFORE calling SetFieldValue/persisting.
        // This test pins current domain reality; rework it into a direct handler
        // failure test once handlers land.
        var type = new ContentType("Articles", "articles");
        var title = new FieldDefinition(type.Id, "Title", "title", FieldDataType.Text);
        var item = new ContentItem(type.Id, "hello-world");
        Assert.True(item.Publish().IsSuccess);

        var result = item.SetFieldValue(title, "edited after publish");

        Assert.True(result.IsSuccess);
        Assert.Equal(ContentStatus.Published, item.Status);
    }
}
