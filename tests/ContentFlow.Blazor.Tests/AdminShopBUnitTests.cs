// Component-level render tests for the InteractiveServer areas (issue #13).
//
// Renders Admin and Shop Index components with bUnit. If a component throws
// during render, the exception surfaces here in test output — this is how the
// HTTP 500 on /admin and /shop is diagnosed without ad-hoc servers.
// These tests are permanent regression guards: DO NOT DELETE.

using Bunit;
using AdminIndex = ContentFlow.Blazor.Web.Components.Admin.Pages.Index;
using ShopIndex = ContentFlow.Blazor.Client.Shop.Pages.Index;

namespace ContentFlow.Blazor.Tests;

public sealed class AdminShopBUnitTests : IDisposable
{
    private readonly Bunit.TestContext _ctx = new();

    [Fact]
    public void Admin_RendersDashboardMarkup()
    {
        var cut = _ctx.RenderComponent<AdminIndex>();

        Assert.Contains("<h1>Admin Dashboard</h1>", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Interactive Server", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Click me (0)", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_CounterIncrements()
    {
        var cut = _ctx.RenderComponent<AdminIndex>();

        cut.Find("button").Click();

        Assert.Contains("Click me (1)", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shop_RendersShopMarkup()
    {
        var cut = _ctx.RenderComponent<ShopIndex>();

        Assert.Contains("<h1>Shop</h1>", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Interactive Auto", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Add to Cart (0)", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shop_CounterIncrements()
    {
        var cut = _ctx.RenderComponent<ShopIndex>();

        cut.Find("button").Click();

        Assert.Contains("Add to Cart (1)", cut.Markup, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }
}
