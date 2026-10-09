// Content repository round-trip tests (issue #7, ADR-007: Infra tests run against
// real PostgreSQL via Testcontainers — no SQLite assumption).
//
// Covers the freshly landed EF Core repositories
// (Infra.Persistence.Repositories.ContentTypeRepository/ContentItemRepository):
// MigrateAsync applies the real migrations to an ephemeral postgres:16-alpine
// container, then a content type with fields and an item with typed values round-trip
// through Add/SaveChangesAsync and read back with their navigations. Docker gating is
// discovery-time (xUnit 2.9.3 has no runtime Skip API), mirroring the E2E suite's
// RequiresDockerFact pattern without coupling to the E2E assembly.

using System.IO.Pipes;
using ContentFlow.Domain.Content;
using ContentFlow.Infra.Persistence;
using ContentFlow.Infra.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ContentFlow.Infra.Tests.Content;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!DockerProbe.IsPresent())
        {
            Skip = "Infra prerequisite unavailable: no Docker endpoint found. " +
                "Start Docker Desktop (or set DOCKER_HOST) and re-run.";
        }
    }
}

internal static class DockerProbe
{
    public static bool IsPresent()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return true;
        }

        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var pipe = new NamedPipeClientStream(
                    ".", "docker_engine", PipeDirection.InOut, PipeOptions.Asynchronous);
                var connect = pipe.ConnectAsync(1000);
                return connect.Wait(1500) && pipe.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        return File.Exists("/var/run/docker.sock");
    }
}

public sealed class ContentRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private DbContextOptions<ContentFlowDbContext> Options =>
        new DbContextOptionsBuilder<ContentFlowDbContext>()
            .UseContentFlowNpgsql(_container.GetConnectionString())
            .Options;

    private ContentFlowDbContext NewContext() => new(Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [RequiresDockerFact]
    public async Task ContentType_MigrateAndRoundTrip_WithFields()
    {
        await using var db = NewContext();
        var types = new ContentTypeRepository(db);

        var type = new ContentType("Articles", "articles");
        Assert.True(type.AddField(new FieldDefinition(type.Id, "Title", "title", FieldDataType.Text, isRequired: true)).IsSuccess);
        Assert.True(type.AddField(new FieldDefinition(type.Id, "Views", "views", FieldDataType.Number)).IsSuccess);

        types.Add(type);
        await types.SaveChangesAsync();

        var byId = await types.GetByIdAsync(type.Id);
        Assert.NotNull(byId);
        Assert.Equal("articles", byId.Slug);
        Assert.Equal(2, byId.Fields.Count);

        // Slug lookup normalizes like the contract requires.
        var bySlug = await types.GetBySlugAsync("ARTICLES");
        Assert.NotNull(bySlug);
        Assert.Equal(type.Id, bySlug.Id);

        Assert.True(await types.SlugExistsAsync("articles"));
        Assert.False(await types.SlugExistsAsync("articles", excludeId: type.Id));
        Assert.False(await types.SlugExistsAsync("nope"));
    }

    [RequiresDockerFact]
    public async Task ContentItem_MigrateAndRoundTrip_WithTypedValuesAndStatusFilter()
    {
        await using var db = NewContext();
        var types = new ContentTypeRepository(db);
        var items = new ContentItemRepository(db);

        var type = new ContentType("Posts", "posts");
        var title = new FieldDefinition(type.Id, "Title", "title", FieldDataType.Text, isRequired: true);
        var views = new FieldDefinition(type.Id, "Views", "views", FieldDataType.Number);
        Assert.True(type.AddField(title).IsSuccess);
        Assert.True(type.AddField(views).IsSuccess);
        types.Add(type);
        await types.SaveChangesAsync();

        var item = new ContentItem(type.Id, "hello-world");
        Assert.True(item.SetFieldValue(title, "Hello").IsSuccess);
        Assert.True(item.SetFieldValue(views, "42").IsSuccess);
        items.Add(item);
        await items.SaveChangesAsync();

        var byId = await items.GetByIdAsync(item.Id);
        Assert.NotNull(byId);
        Assert.Equal(ContentStatus.Draft, byId.Status);
        Assert.Equal(2, byId.FieldValues.Count);
        Assert.Equal("Hello", byId.FieldValues.Single(v => v.FieldDefinitionId == title.Id).TextValue);
        Assert.Equal(42m, byId.FieldValues.Single(v => v.FieldDefinitionId == views.Id).NumberValue);
        Assert.NotNull(byId.ContentType);
        Assert.Equal(2, byId.ContentType.Fields.Count);

        Assert.True(await items.SlugExistsAsync(type.Id, "hello-world"));
        Assert.False(await items.SlugExistsAsync(type.Id, "hello-world", excludeId: item.Id));
        Assert.False(await items.SlugExistsAsync(Guid.NewGuid(), "hello-world"));

        var drafts = await items.ListByTypeAsync(type.Id, ContentStatus.Draft);
        Assert.Single(drafts);
        var published = await items.ListByTypeAsync(type.Id, ContentStatus.Published);
        Assert.Empty(published);

        Assert.True(item.Publish().IsSuccess);
        await items.SaveChangesAsync();
        Assert.Single(await items.ListByTypeAsync(type.Id, ContentStatus.Published));
    }
}
