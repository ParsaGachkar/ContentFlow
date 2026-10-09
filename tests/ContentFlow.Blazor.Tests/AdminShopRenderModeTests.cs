// Representative InteractiveServer render-mode tests (ADR-001, ADR-007).
//
// /admin and /shop MUST carry the InteractiveServer render mode attribute.
// Uses xUnit + component reflection fallback because bUnit is not referenced
// by ContentFlow.Blazor.Tests.csproj (see report to main for the one-line
// opt-in). No new packages required for these tests.

using ContentFlow.Blazor.Web.Components.Admin.Layout;
using ContentFlow.Blazor.Web.Components.Shop.Layout;
using AdminIndex = ContentFlow.Blazor.Web.Components.Admin.Pages.Index;
using ShopIndex = ContentFlow.Blazor.Web.Components.Shop.Pages.Index;

namespace ContentFlow.Blazor.Tests;

/// <summary>
/// Proves the Admin and Shop areas opt into InteractiveServer rendering
/// while keeping their own layouts and routes.
/// </summary>
public sealed class AdminShopRenderModeTests
{
    private static readonly Type AdminType = typeof(AdminIndex);
    private static readonly Type ShopType = typeof(ShopIndex);

    [Fact]
    public void Admin_HasAdminRoute()
    {
        Assert.Equal("/admin", GetRouteTemplate(AdminType));
    }

    [Fact]
    public void Admin_UsesAdminLayout()
    {
        Assert.Equal(typeof(AdminLayout), GetLayoutType(AdminType));
    }

    [Fact]
    public void Admin_CarriesInteractiveServerRenderMode()
    {
        Assert.True(
            HasInteractiveServerRenderMode(AdminType),
            $"Admin Index must opt into InteractiveServer. Found render-mode attributes: [{string.Join(", ", GetRenderModeAttributeNames(AdminType))}]");
    }

    [Fact]
    public void Shop_HasShopRoute()
    {
        Assert.Equal("/shop", GetRouteTemplate(ShopType));
    }

    [Fact]
    public void Shop_UsesShopLayout()
    {
        Assert.Equal(typeof(ShopLayout), GetLayoutType(ShopType));
    }

    [Fact]
    public void Shop_CarriesInteractiveServerRenderMode()
    {
        Assert.True(
            HasInteractiveServerRenderMode(ShopType),
            $"Shop Index must opt into InteractiveServer. Found render-mode attributes: [{string.Join(", ", GetRenderModeAttributeNames(ShopType))}]");
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

    // The Razor compiler emits `@rendermode InteractiveServer` as a compiler-generated
    // attribute (e.g. `__PrivateComponentRenderModeAttribute`), NOT as a literal
    // `RenderModeInteractiveServerAttribute`. Inspect constructor + named arguments
    // (via CustomAttributeData, without instantiating) for an InteractiveServer mode.
    private static bool HasInteractiveServerRenderMode(Type componentType)
    {
        foreach (var data in System.Reflection.CustomAttributeData.GetCustomAttributes(componentType))
        {
            if (!data.AttributeType.Name.Contains("RenderMode", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var arg in data.ConstructorArguments)
            {
                if (ValueMentionsInteractiveServer(arg.Value))
                {
                    return true;
                }
            }

            foreach (var named in data.NamedArguments)
            {
                if (ValueMentionsInteractiveServer(named.TypedValue.Value))
                {
                    return true;
                }
            }

            // Attribute exists but carries no inspectable mode value (compiler-generated
            // holder). Fall back to the instantiated attribute's members below.
            var instance = componentType
                .GetCustomAttributes(inherit: false)
                .FirstOrDefault(a => a.GetType() == data.AttributeType);
            if (instance is not null)
            {
                var members = instance.GetType().GetProperties(
                        System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance)
                    .Select(p => p.GetValue(instance))
                    .Concat(instance.GetType().GetFields(
                            System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance)
                        .Select(f => f.GetValue(instance)));

                if (members.Any(ValueMentionsInteractiveServer))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ValueMentionsInteractiveServer(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is System.Collections.IEnumerable items and not string)
        {
            foreach (var item in items)
            {
                if (ValueMentionsInteractiveServer(item))
                {
                    return true;
                }
            }

            return false;
        }

        var text = value.ToString();
        return text is not null
            && text.Contains("InteractiveServer", StringComparison.OrdinalIgnoreCase);
    }
}
