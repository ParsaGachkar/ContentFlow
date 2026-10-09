// Representative SSR-default tests (ADR-001, ADR-007).
//
// Public Home MUST render as static SSR: no interactive render-mode attribute.
// Uses xUnit + component reflection fallback because bUnit is not referenced
// by ContentFlow.Blazor.Tests.csproj (see report to main for the one-line
// opt-in: <PackageReference Include="bunit" /> — version 1.40.0 already pinned
// in Directory.Packages.props). No new packages required for these tests.

using System.Reflection;
using ContentFlow.Blazor.Web.Components.Layout;
using ContentFlow.Blazor.Web.Components.Pages;

namespace ContentFlow.Blazor.Tests;

/// <summary>
/// Proves the public homepage renders with static SSR by default
/// (route "/", MainLayout, and no interactive render-mode attribute,
/// hence no Blazor circuit required).
/// </summary>
public sealed class SsrRenderingTests
{
    private static readonly Type HomeType = typeof(Home);

    [Fact]
    public void Home_HasRootRoute()
    {
        Assert.Equal("/", GetRouteTemplate(HomeType));
    }

    [Fact]
    public void Home_UsesMainLayout()
    {
        Assert.Equal(typeof(MainLayout), GetLayoutType(HomeType));
    }

    [Fact]
    public void Home_HasNoInteractiveRenderModeAttribute()
    {
        // Static SSR = absence of any [RenderMode*] attribute on the component.
        // InteractiveServer / InteractiveWebAssembly / InteractiveAuto would
        // require a circuit (or WASM runtime); Home must need neither.
        Assert.Empty(GetRenderModeAttributeNames(HomeType));
    }

    [Fact]
    public void Home_DoesNotOptIntoStreamingRendering()
    {
        // Streaming rendering is opt-in; the static public page must not use it.
        Assert.DoesNotContain(
            HomeType.GetCustomAttributes(inherit: false),
            a => string.Equals(
                a.GetType().FullName,
                "Microsoft.AspNetCore.Components.Sections.StreamRenderingAttribute",
                StringComparison.Ordinal));
    }

    private static string? GetRouteTemplate(Type componentType)
    {
        var attribute = componentType
            .GetCustomAttributes(inherit: false)
            .FirstOrDefault(a => string.Equals(
                a.GetType().FullName,
                "Microsoft.AspNetCore.Components.RouteAttribute",
                StringComparison.Ordinal));

        Assert.NotNull(attribute);
        return attribute.GetType().GetProperty("Template")?.GetValue(attribute) as string;
    }

    private static Type? GetLayoutType(Type componentType)
    {
        var attribute = componentType
            .GetCustomAttributes(inherit: false)
            .FirstOrDefault(a => string.Equals(
                a.GetType().FullName,
                "Microsoft.AspNetCore.Components.LayoutAttribute",
                StringComparison.Ordinal));

        Assert.NotNull(attribute);
        return attribute.GetType().GetProperty("LayoutType")?.GetValue(attribute) as Type;
    }

    private static IReadOnlyList<string> GetRenderModeAttributeNames(Type componentType)
    {
        return componentType
            .GetCustomAttributes(inherit: false)
            .Select(a => a.GetType().Name)
            .Where(name => name.Contains("RenderMode", StringComparison.Ordinal))
            .ToList();
    }
}
