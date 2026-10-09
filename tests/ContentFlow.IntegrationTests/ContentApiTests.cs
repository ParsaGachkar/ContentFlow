// Headless content API tests (issue #8, ADR-004/ADR-007).
//
// Contract under test (src/Endpoints/ContentApi.cs + Application read handlers):
// - Anonymous: published lists/items are 200; drafts and unknown types are 404.
// - ?includeDrafts=true on a draft: 200 only with content.read (else 404 —
//   existence hiding, never 403); invalid API key is 401.
// - Invalid paging is 400. OpenAPI/Scalar presence is covered by E2E.
//
// Repositories + permission checker are NSubstitute doubles injected AFTER the
// app's own registrations (last registration wins), so the REAL handlers,
// validators, and JSON error mapping execute. One fixture per class: tests in
// this class run sequentially and reconfigure the doubles per test.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ContentFlow.IntegrationTests;

public sealed class ContentApiFactory : WebApplicationFactory<Program>
{
    public IContentTypeRepository Types { get; } = Substitute.For<IContentTypeRepository>();

    public IContentItemRepository Items { get; } = Substitute.For<IContentItemRepository>();

    public IPermissionChecker Permissions { get; } = Substitute.For<IPermissionChecker>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IContentTypeRepository>(_ => Types);
            services.AddScoped<IContentItemRepository>(_ => Items);
            services.AddScoped<IPermissionChecker>(_ => Permissions);
        });
    }
}

public sealed class ContentApiTests : IClassFixture<ContentApiFactory>
{
    private readonly ContentApiFactory _factory;
    private readonly HttpClient _client;

    public ContentApiTests(ContentApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static ContentType ArticlesType()
    {
        var type = new ContentType("Articles", "articles");
        Assert.True(type.AddField(new FieldDefinition(type.Id, "Title", "title", FieldDataType.Text, isRequired: true)).IsSuccess);
        return type;
    }

    private static void AttachType(ContentItem item, ContentType type)
    {
        // Mocks don't do EF relationship fixup, but GetByIdAsync's contract promises
        // ContentType (+Fields) loaded — mirror that graph explicitly so ToDto()
        // resolves value keys exactly as with EF materialization.
        typeof(ContentItem).GetProperty(nameof(ContentItem.ContentType))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(item, [type]);
    }

    private static ContentItem PublishedHello(ContentType type)
    {
        var item = new ContentItem(type.Id, "hello");
        Assert.True(item.SetFieldValue(type.Fields.Single(f => f.Key == "title"), "Hello").IsSuccess);
        Assert.True(item.Publish().IsSuccess);
        AttachType(item, type);
        return item;
    }

    private static ContentItem DraftSecret(ContentType type)
    {
        var item = new ContentItem(type.Id, "secret");
        Assert.True(item.SetFieldValue(type.Fields.Single(f => f.Key == "title"), "Shh").IsSuccess);
        AttachType(item, type);
        return item;
    }

    private void DenyAll()
    {
        _factory.Permissions
            .HasAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task List_Anonymous_ReturnsOnlyPublishedEnvelope()
    {
        DenyAll();
        var type = ArticlesType();
        var hello = PublishedHello(type);
        _factory.Types.GetBySlugAsync("articles", Arg.Any<CancellationToken>()).Returns(type);
        _factory.Items.ListByTypeAsync(type.Id, ContentStatus.Published, Arg.Any<CancellationToken>())
            .Returns(new List<ContentItem> { hello });

        using var response = await _client.GetAsync("/api/v1/content/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("total").GetInt32());
        var slugs = root.GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("slug").GetString())
            .ToList();
        Assert.Equal(new[] { "hello" }, slugs);
    }

    [Fact]
    public async Task Get_Published_Anonymous_Returns200WithValues()
    {
        DenyAll();
        var type = ArticlesType();
        var hello = PublishedHello(type);
        _factory.Types.GetBySlugAsync("articles", Arg.Any<CancellationToken>()).Returns(type);
        _factory.Items.GetBySlugAsync(type.Id, "hello", ContentStatus.Published, Arg.Any<CancellationToken>())
            .Returns(hello);

        using var response = await _client.GetAsync("/api/v1/content/articles/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal("hello", document.RootElement.GetProperty("slug").GetString());
        Assert.Equal("Hello", document.RootElement.GetProperty("values").GetProperty("title").GetString());
    }

    [Fact]
    public async Task Get_Draft_Anonymous_Returns404()
    {
        DenyAll();
        var type = ArticlesType();
        _factory.Types.GetBySlugAsync("articles", Arg.Any<CancellationToken>()).Returns(type);
        _factory.Items.GetBySlugAsync(type.Id, "secret", ContentStatus.Published, Arg.Any<CancellationToken>())
            .Returns((ContentItem?)null);

        using var response = await _client.GetAsync("/api/v1/content/articles/secret");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_UnknownType_Returns404()
    {
        DenyAll();
        _factory.Types.GetBySlugAsync("nope", Arg.Any<CancellationToken>()).Returns((ContentType?)null);

        using var response = await _client.GetAsync("/api/v1/content/nope/hello");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_InvalidPage_Returns400()
    {
        DenyAll();

        using var response = await _client.GetAsync("/api/v1/content/articles?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Draft_InvalidKey_Returns401()
    {
        DenyAll();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/content/articles/secret?includeDrafts=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", "invalid-key");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Draft_WithReadPermission_Returns200()
    {
        var type = ArticlesType();
        var secret = DraftSecret(type);
        _factory.Types.GetBySlugAsync("articles", Arg.Any<CancellationToken>()).Returns(type);
        _factory.Permissions
            .HasAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>(), PermissionCodes.ContentRead, Arg.Any<CancellationToken>())
            .Returns(true);
        _factory.Items.GetBySlugAsync(type.Id, "secret", null, Arg.Any<CancellationToken>())
            .Returns(secret);

        using var response = await _client.GetAsync("/api/v1/content/articles/secret?includeDrafts=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(item);
        Assert.Equal("secret", item["slug"].GetString());
    }
}
