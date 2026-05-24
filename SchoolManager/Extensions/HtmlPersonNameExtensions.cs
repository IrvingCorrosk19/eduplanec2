using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManager.Helpers;

namespace SchoolManager.Extensions;

public static class HtmlPersonNameExtensions
{
    public static IHtmlContent PersonName(this IHtmlHelper html, string? value) =>
        new HtmlString(System.Net.WebUtility.HtmlEncode(DisplayNameHelper.ToDisplayPersonName(value)));

    public static IHtmlContent PersonFullName(this IHtmlHelper html, string? firstName, string? lastName) =>
        new HtmlString(System.Net.WebUtility.HtmlEncode(DisplayNameHelper.FormatFullName(firstName, lastName)));

    public static IHtmlContent PersonFullName(this IHtmlHelper html, string? combinedFullName) =>
        new HtmlString(System.Net.WebUtility.HtmlEncode(DisplayNameHelper.FormatFullName(combinedFullName)));
}
