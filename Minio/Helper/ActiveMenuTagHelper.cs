using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

public static class HtmlExtensions
{
    public static string IsActive(this IHtmlHelper htmlHelper, string path)
    {
        var currentPath = htmlHelper.ViewContext.HttpContext.Request.Path.ToString();
        return currentPath.Equals(path, StringComparison.OrdinalIgnoreCase) ? "active" : "";
    }

    public static string IsMenuOpen(this IHtmlHelper htmlHelper, string submenuPath)
    {
        var currentPath = htmlHelper.ViewContext.HttpContext.Request.Path.ToString();
        return currentPath.Equals(submenuPath, StringComparison.OrdinalIgnoreCase) ? "menu-open" : "";
    }
}